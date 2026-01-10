using Collectify.App.Commands;
using Collectify.Model.Entities;
using Collectify.Model.Enums;
using Collectify.Model.InputModels;
using Collectify.Model.Interfaces;
using Collectify.App;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Collectify.App.ViewModels;

public class EditTemplateViewModel : INotifyPropertyChanged
{
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
    public string NewColumnName { get => _newColumnName; set { _newColumnName = value; OnPropertyChanged(); } }

    private FieldType _selectedFieldType = FieldType.Text;
    public FieldType SelectedFieldType { get => _selectedFieldType; set { _selectedFieldType = value; OnPropertyChanged(); } }

    private readonly ObservableCollection<ColumnItem> _removedColumns = new();

    public ICommand AddColumnCommand { get; }
    public ICommand RemoveColumnCommand { get; }
    public ICommand SaveTemplateCommand { get; }
    public ICommand DeleteTemplateCommand { get; }

    public bool CanSubmit => CanSave();

    public EditTemplateViewModel(ITemplateService templateService)
    {
        _templateService = templateService;

        AddColumnCommand = new RelayCommand(AddColumn);
        RemoveColumnCommand = new RelayCommand<ColumnItem>(RemoveColumn);

        SaveTemplateCommand = new AsyncRelayCommand(SaveAsync, () => CanSubmit);
        DeleteTemplateCommand = new AsyncRelayCommand(DeleteAsync, CanDelete);

        Columns.CollectionChanged += OnColumnsChanged;

        LoadTemplatesAsync();
    }

    private void RefreshSaveState()
    {
        OnPropertyChanged(nameof(CanSubmit));
        (SaveTemplateCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
    }

    private void OnColumnsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshSaveState();
    }

    private bool IsDirty()
    {
        if (SelectedTemplate == null) return false;

        bool nameChanged = TemplateName != SelectedTemplate.Name;
        bool columnsRemoved = _removedColumns.Any();
        bool columnsAdded = Columns.Any(c => !c.Id.HasValue);

        return nameChanged || columnsRemoved || columnsAdded;
    }

    private bool CanSave()
    {
        return SelectedTemplate != null &&
               !string.IsNullOrWhiteSpace(TemplateName) &&
               Columns.Any() &&
               IsDirty();
    }

    private async void LoadTemplatesAsync()
    {
        var templates = await _templateService.GetAllTemplatesAsync();
        TemplateList.Clear();
        foreach (var t in templates)
            TemplateList.Add(t);

        if (TemplateList.Any())
            SelectedTemplate = TemplateList.First();
    }

    private async void LoadTemplateFields()
    {
        Columns.Clear();
        _removedColumns.Clear();
        if (SelectedTemplate == null) return;

        var fullTemplate = await _templateService.GetTemplateAsync(SelectedTemplate.Id, includeFields: true);
        if (fullTemplate == null) return;

        _templateName = fullTemplate.Name;
        OnPropertyChanged(nameof(TemplateName));

        foreach (var f in fullTemplate.Fields)
        {
            Columns.Add(new ColumnItem
            {
                Id = f.Id,
                Name = f.Name,
                DataType = f.FieldType
            });
        }

        RefreshSaveState();
    }

    private void AddColumn()
    {
        if (string.IsNullOrWhiteSpace(NewColumnName)) return;
        if (Columns.Any(c => c.Name.Equals(NewColumnName, StringComparison.OrdinalIgnoreCase))) return;

        Columns.Add(new ColumnItem
        {
            Name = NewColumnName.Trim(),
            DataType = SelectedFieldType
        });
        NewColumnName = string.Empty;
    }

    private void RemoveColumn(ColumnItem item)
    {
        if (!Columns.Contains(item)) return;

        if (item.Id.HasValue)
            _removedColumns.Add(item);

        Columns.Remove(item);
    }

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
                    SelectedTemplate.Id, col.Name, col.DataType, false);
                col.Id = newField.Id;
            }

            CloseAction?.Invoke();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving template: {ex.Message}";
            StatusType = StatusMessageType.Error;
        }
    }

    private bool CanDelete() => SelectedTemplate != null;

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

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}