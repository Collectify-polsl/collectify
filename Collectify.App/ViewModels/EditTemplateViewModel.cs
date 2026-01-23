using Collectify.App;
using Collectify.App.Commands;
using Collectify.Model.Entities;
using Collectify.Model.Enums;
using Collectify.Model.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Collectify.App.ViewModels;

// Manages the logic for modifying existing templates, including adding, removing, and updating field definitions.
public class EditTemplateViewModel : INotifyPropertyChanged
{
    private const string ReferenceFieldName = "itemReference";

    private readonly ITemplateService _templateService;

    public Action? CloseAction { get; set; }

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    private StatusMessageType _statusType;
    public StatusMessageType StatusType
    {
        get => _statusType;
        set { _statusType = value; OnPropertyChanged(); }
    }

    public ObservableCollection<Template> TemplateList { get; } = new();
    private Template? _selectedTemplate;
    private string _templateName = string.Empty;

    public Template? SelectedTemplate
    {
        get => _selectedTemplate;
        set
        {
            _selectedTemplate = value;
            OnPropertyChanged();
            LoadTemplateFields();
            RefreshSaveState();
            (DeleteTemplateCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    public string TemplateName
    {
        get => _templateName;
        set
        {
            _templateName = value;
            OnPropertyChanged();
            RefreshSaveState();
        }
    }

    public ObservableCollection<ColumnItem> Columns { get; } = new();

    public ObservableCollection<FieldType> DataTypeList { get; } = new()
    {
        FieldType.Text, FieldType.Integer, FieldType.Decimal,
        FieldType.Date, FieldType.Image, FieldType.ItemReference
    };

    private string _newColumnName = string.Empty;
    public string NewColumnName
    {
        get => _newColumnName;
        set => SetNewColumnName(value);
    }

    private bool _newColumnAllowsMultiple;
    public bool NewColumnAllowsMultiple
    {
        get => _newColumnAllowsMultiple;
        set
        {
            if (_newColumnAllowsMultiple == value) return;
            _newColumnAllowsMultiple = value;
            OnPropertyChanged();
        }
    }

    public bool ShowMultiReferenceToggle => SelectedFieldType == FieldType.ItemReference;

    public bool IsColumnNameReadOnly => SelectedFieldType == FieldType.ItemReference;

    private FieldType _selectedFieldType = FieldType.Text;
    public FieldType SelectedFieldType
    {
        get => _selectedFieldType;
        set
        {
            if (_selectedFieldType == value) return;

            _selectedFieldType = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsColumnNameReadOnly));
            OnPropertyChanged(nameof(ShowMultiReferenceToggle));

            if (value == FieldType.ItemReference)
            {
                SetNewColumnName(ReferenceFieldName);
            }
            else
            {
                SetNewColumnName(string.Empty);
                NewColumnAllowsMultiple = false;
            }
        }
    }

    private readonly ObservableCollection<ColumnItem> _removedColumns = new();

    public ICommand AddColumnCommand { get; }
    public ICommand RemoveColumnCommand { get; }
    public ICommand SaveTemplateCommand { get; }
    public ICommand DeleteTemplateCommand { get; }

    public bool CanSubmit => CanSave();

    public EditTemplateViewModel(ITemplateService templateService)
    {
        _templateService = templateService;

        AddColumnCommand = new RelayCommand(AddColumn, CanAddColumn);
        RemoveColumnCommand = new RelayCommand<ColumnItem>(RemoveColumn);

        SaveTemplateCommand = new AsyncRelayCommand(SaveAsync, () => CanSubmit);
        DeleteTemplateCommand = new AsyncRelayCommand(DeleteAsync, CanDelete);

        Columns.CollectionChanged += OnColumnsChanged;

        LoadTemplatesAsync();
    }

    // Updates the UI command states to reflect whether changes are eligible for saving.
    private void RefreshSaveState()
    {
        OnPropertyChanged(nameof(CanSubmit));
        (SaveTemplateCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
    }

    // Handles synchronization of property change listeners when the collection of template columns is modified.
    private void OnColumnsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (ColumnItem column in e.NewItems)
                column.PropertyChanged += ColumnOnPropertyChanged;
        }

        if (e.OldItems != null)
        {
            foreach (ColumnItem column in e.OldItems)
                column.PropertyChanged -= ColumnOnPropertyChanged;
        }

        RefreshSaveState();
    }

    // Monitors individual column properties to trigger a UI state refresh when settings like "IsList" change.
    private void ColumnOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ColumnItem.IsList) ||
            e.PropertyName == nameof(ColumnItem.HasListChange))
        {
            RefreshSaveState();
        }
    }

    // Compares the current UI state against the original template data to detect unsaved modifications.
    private bool IsDirty()
    {
        if (SelectedTemplate == null) return false;

        bool nameChanged = TemplateName != SelectedTemplate.Name;
        bool columnsRemoved = _removedColumns.Any();
        bool columnsAdded = Columns.Any(c => !c.Id.HasValue);
        bool listChanged = Columns.Any(c => c.HasListChange);

        return nameChanged || columnsRemoved || columnsAdded || listChanged;
    }

    // Validates that the template is in a valid state and contains unsaved changes before allowing a save.
    private bool CanSave()
    {
        return SelectedTemplate != null &&
               !string.IsNullOrWhiteSpace(TemplateName) &&
               Columns.Any() &&
               IsDirty();
    }

    // Populates the template selection list from the database and selects the first entry by default.
    private async void LoadTemplatesAsync()
    {
        var templates = await _templateService.GetAllTemplatesAsync();
        TemplateList.Clear();
        foreach (var t in templates)
            TemplateList.Add(t);

        if (TemplateList.Any())
            SelectedTemplate = TemplateList.First();
    }

    // Fetches detailed field information for the selected template and populates the editor columns.
    private async void LoadTemplateFields()
    {
        foreach (var column in Columns)
            column.PropertyChanged -= ColumnOnPropertyChanged;

        Columns.Clear();
        _removedColumns.Clear();
        if (SelectedTemplate == null) return;

        var fullTemplate = await _templateService.GetTemplateAsync(SelectedTemplate.Id, includeFields: true);
        if (fullTemplate == null) return;

        _templateName = fullTemplate.Name;
        OnPropertyChanged(nameof(TemplateName));

        foreach (var f in fullTemplate.Fields)
        {
            var column = new ColumnItem
            {
                Id = f.Id,
                Name = f.Name,
                DataType = f.FieldType,
                IsList = f.IsList,
                InitialIsList = f.IsList
            };
            column.PropertyChanged += ColumnOnPropertyChanged;
            Columns.Add(column);
        }

        RefreshSaveState();
    }

    // Verifies that a new column name is provided and doesn't already exist in the template.
    private bool CanAddColumn() =>
        !string.IsNullOrWhiteSpace(NewColumnName) &&
        !Columns.Any(c => c.Name.Equals(NewColumnName, StringComparison.OrdinalIgnoreCase));

    // Creates a new column definition and adds it to the current template configuration.
    private void AddColumn()
    {
        if (!CanAddColumn()) return;

        bool isList = SelectedFieldType == FieldType.ItemReference && NewColumnAllowsMultiple;

        var column = new ColumnItem
        {
            Name = NewColumnName,
            DataType = SelectedFieldType,
            IsList = isList,
            InitialIsList = isList
        };
        column.PropertyChanged += ColumnOnPropertyChanged;
        Columns.Add(column);

        SetNewColumnName(string.Empty);
        NewColumnAllowsMultiple = false;
    }

    // Removes a column from the UI and tracks it for permanent deletion from the database upon saving.
    private void RemoveColumn(ColumnItem item)
    {
        if (!Columns.Contains(item)) return;

        if (item.Id.HasValue)
            _removedColumns.Add(item);

        item.PropertyChanged -= ColumnOnPropertyChanged;
        Columns.Remove(item);
    }

    // Persists all pending template name changes, added fields, removed fields, and modified field settings to the database.
    private async Task SaveAsync()
    {
        if (!CanSave()) return;

        try
        {
            StatusMessage = string.Empty;
            if (TemplateName != SelectedTemplate!.Name)
                await _templateService.UpdateTemplateAsync(SelectedTemplate.Id, TemplateName);

            foreach (var removed in _removedColumns)
            {
                if (removed.Id.HasValue)
                    await _templateService.RemoveFieldAsync(removed.Id.Value);
            }
            _removedColumns.Clear();

            foreach (var col in Columns.Where(c => !c.Id.HasValue))
            {
                var newField = await _templateService.AddFieldAsync(
                    SelectedTemplate.Id, col.Name, col.DataType, col.IsList);
                col.Id = newField.Id;
                col.InitialIsList = col.IsList;
            }

            var changedColumns = Columns.Where(c => c.HasListChange).ToList();
            foreach (var column in changedColumns)
            {
                await _templateService.UpdateFieldAsync(column.Id!.Value, column.IsList);
                column.InitialIsList = column.IsList;
            }

            RefreshSaveState();
            CloseAction?.Invoke();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving template: {ex.Message}";
            StatusType = StatusMessageType.Error;
        }
    }

    // Confirms if a template is selected and available for deletion.
    private bool CanDelete() => SelectedTemplate != null;

    // Requests user confirmation and permanently removes the selected template and its associated fields.
    private async Task DeleteAsync()
    {
        if (SelectedTemplate == null) return;

        var window = new ConfirmationWindow($"Delete template '{TemplateName}'?", "Confirm");
        window.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);

        if (window.ShowDialog() != true) return;

        try
        {
            StatusMessage = string.Empty;
            await _templateService.DeleteTemplateAsync(SelectedTemplate.Id);
            LoadTemplatesAsync();
            StatusMessage = "Template deleted.";
            StatusType = StatusMessageType.Success;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            StatusType = StatusMessageType.Error;
        }
    }

    // Updates the staged column name while ensuring special field types maintain their reserved naming conventions.
    private void SetNewColumnName(string? value)
    {
        var normalized = NormalizeColumnName(value);
        if (_newColumnName == normalized) return;

        _newColumnName = normalized;
        OnPropertyChanged(nameof(NewColumnName));
        (AddColumnCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    // Standardizes column names by trimming whitespace or enforcing fixed names for reference types.
    private string NormalizeColumnName(string? value) =>
        SelectedFieldType == FieldType.ItemReference
            ? ReferenceFieldName
            : (value ?? string.Empty).Trim();

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}