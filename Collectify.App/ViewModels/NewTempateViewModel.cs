using Collectify.App.Commands;
using Collectify.App;
using Collectify.Model.Enums;
using Collectify.Model.InputModels;
using Collectify.Model.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Linq;
using System.Threading.Tasks;

namespace Collectify.App.ViewModels;

/// <summary>
/// View model for creating a new template with custom fields.
/// </summary>
public class NewTemplateViewModel : INotifyPropertyChanged
{
    private readonly ITemplateService _templateService;

    /// <summary>
    /// Action to close the window.
    /// </summary>
    public Action? CloseAction { get; set; }

    private string _statusMessage = string.Empty;
    /// <summary>
    /// Gets or sets the status message to display.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    private StatusMessageType _statusType;
    /// <summary>
    /// Gets or sets the type of the status message.
    /// </summary>
    public StatusMessageType StatusType
    {
        get => _statusType;
        set { _statusType = value; OnPropertyChanged(); }
    }

    private string _templateName = string.Empty;
    /// <summary>
    /// Gets or sets the name of the new template.
    /// </summary>
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
    /// <summary>
    /// Gets or sets the name for a new column to be added.
    /// </summary>
    public string NewColumnName
    {
        get => _newColumnName;
        set
        {
            _newColumnName = value;
            OnPropertyChanged();
            (AddColumnCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// Gets the list of available data types.
    /// </summary>
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
    /// <summary>
    /// Gets or sets the selected data type for the new column.
    /// </summary>
    public FieldType SelectedFieldType
    {
        get => _selectedFieldType;
        set { _selectedFieldType = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Gets an error message if the template cannot be saved, or null otherwise.
    /// </summary>
    public string? ErrorMessage => CanSave() ? null : "Please fill in all fields.";

    /// <summary>
    /// Gets the list of columns defined for the new template.
    /// </summary>
    public ObservableCollection<ColumnItem> Columns { get; } = new();

    /// <summary>
    /// Command to add a new column to the list.
    /// </summary>
    public ICommand AddColumnCommand { get; }
    
    /// <summary>
    /// Command to remove a column from the list.
    /// </summary>
    public ICommand RemoveColumnCommand { get; }
    
    /// <summary>
    /// Command to create the template.
    /// </summary>
    public ICommand CreateCommand { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="NewTemplateViewModel"/> class.
    /// </summary>
    /// <param name="templateService">The service for template operations.</param>
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

    private bool CanAddColumn() =>
        !string.IsNullOrWhiteSpace(NewColumnName) &&
        !Columns.Any(c =>
            c.Name.Equals(NewColumnName.Trim(), StringComparison.OrdinalIgnoreCase));

    private void AddColumn()
    {
        Columns.Add(new ColumnItem
        {
            Name = NewColumnName.Trim(),
            DataType = SelectedFieldType
        });
        NewColumnName = string.Empty;
    }

    private void RemoveColumn(ColumnItem column)
    {
        if (Columns.Contains(column))
            Columns.Remove(column);
    }

    private bool CanSave() =>
        !string.IsNullOrWhiteSpace(TemplateName) &&
        Columns.Any();

    private async Task SaveTemplateAsync()
    {
        try
        {
            StatusMessage = string.Empty;
            var fields = Columns.Select(c =>
                new TemplateFieldDefinitionInput
                {
                    Name = c.Name,
                    FieldType = c.DataType
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

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

