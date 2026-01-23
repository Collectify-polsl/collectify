using Collectify.App;
using Collectify.App.Commands;
using Collectify.Model.Enums;
using Collectify.Model.InputModels;
using Collectify.Model.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Collectify.App.ViewModels;

// Manages the logic for defining a new metadata template, including its name and a collection of custom fields.
public class NewTemplateViewModel : INotifyPropertyChanged
{
    private const string ReferenceFieldName = "itemReference";
    private const string DefaultStatusMessage = "Ready";

    private readonly ITemplateService _templateService;

    public Action? CloseAction { get; set; }

    private string _statusMessage = DefaultStatusMessage;
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

    private string _templateName = string.Empty;
    public string TemplateName
    {
        get => _templateName;
        set
        {
            _templateName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ErrorMessage));
            (CreateCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    private string _newColumnName = string.Empty;
    public string NewColumnName
    {
        get => _newColumnName;
        set => SetNewColumnName(value);
    }

    private bool _allowMultipleReferences;
    public bool AllowMultipleReferences
    {
        get => _allowMultipleReferences;
        set
        {
            if (_allowMultipleReferences == value) return;
            _allowMultipleReferences = value;
            OnPropertyChanged();
        }
    }

    public bool ShowMultipleReferenceToggle => SelectedFieldType == FieldType.ItemReference;

    public bool IsColumnNameReadOnly => SelectedFieldType == FieldType.ItemReference;

    public ObservableCollection<FieldType> DataTypeList { get; } = new()
    {
        FieldType.Text,
        FieldType.Integer,
        FieldType.Decimal,
        FieldType.Date,
        FieldType.Image,
        FieldType.ItemReference
    };

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
            OnPropertyChanged(nameof(ShowMultipleReferenceToggle));

            if (value == FieldType.ItemReference)
            {
                SetNewColumnName(ReferenceFieldName);
            }
            else
            {
                SetNewColumnName(string.Empty);
                AllowMultipleReferences = false;
            }
        }
    }

    public string? ErrorMessage => CanSave() ? null : "Please fill in all fields.";

    public ObservableCollection<ColumnItem> Columns { get; } = new();

    public ICommand AddColumnCommand { get; }
    public ICommand RemoveColumnCommand { get; }
    public ICommand CreateCommand { get; }

    public NewTemplateViewModel(ITemplateService templateService)
    {
        _templateService = templateService;

        AddColumnCommand = new RelayCommand(AddColumn, CanAddColumn);
        RemoveColumnCommand = new RelayCommand<ColumnItem>(RemoveColumn);
        CreateCommand = new AsyncRelayCommand(SaveTemplateAsync, CanSave);

        Columns.CollectionChanged += (_, __) =>
        {
            OnPropertyChanged(nameof(ErrorMessage));
            (CreateCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        };
    }

    // Checks if the proposed column name is non-empty and unique within the current list.
    private bool CanAddColumn() =>
        !string.IsNullOrWhiteSpace(NewColumnName) &&
        !Columns.Any(c =>
            c.Name.Equals(NewColumnName, StringComparison.OrdinalIgnoreCase));

    // Adds a new field definition to the template and resets the input fields for the next entry.
    private void AddColumn()
    {
        if (!CanAddColumn()) return;

        bool isList = SelectedFieldType == FieldType.ItemReference && AllowMultipleReferences;

        Columns.Add(new ColumnItem
        {
            Name = NewColumnName,
            DataType = SelectedFieldType,
            IsList = isList,
            InitialIsList = isList
        });

        SetNewColumnName(string.Empty);
        AllowMultipleReferences = false;
    }

    // Removes a previously added field from the template definition list.
    private void RemoveColumn(ColumnItem column)
    {
        if (Columns.Contains(column))
            Columns.Remove(column);
    }

    // Validates that the template has a name and contains at least one field before saving.
    private bool CanSave() =>
        !string.IsNullOrWhiteSpace(TemplateName) &&
        Columns.Any();

    // Sends the finalized template name and field definitions to the service for database persistence.
    private async Task SaveTemplateAsync()
    {
        try
        {
            StatusMessage = string.Empty;
            var fields = Columns.Select(c =>
                new TemplateFieldDefinitionInput
                {
                    Name = c.Name,
                    FieldType = c.DataType,
                    IsList = c.IsList
                }).ToList();

            await _templateService.CreateTemplateAsync(TemplateName, fields);

            CloseAction?.Invoke();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save error: {ex.Message}";
            StatusType = StatusMessageType.Error;
        }
    }

    // Updates the internal column name state and notifies the UI while enforcing naming rules for specific types.
    private void SetNewColumnName(string? value)
    {
        var normalized = NormalizeColumnName(value);
        if (_newColumnName == normalized) return;

        _newColumnName = normalized;
        OnPropertyChanged(nameof(NewColumnName));
        (AddColumnCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    // Formats the column name based on whether it is a standard field or a reserved reference type.
    private string NormalizeColumnName(string? value) =>
        SelectedFieldType == FieldType.ItemReference
            ? ReferenceFieldName
            : (value ?? string.Empty).Trim();

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}