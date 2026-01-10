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

public class NewTemplateViewModel : INotifyPropertyChanged
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
        set
        {
            _newColumnName = value;
            OnPropertyChanged();
            (AddColumnCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

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
        set { _selectedFieldType = value; OnPropertyChanged(); }
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
                    FieldType = c.DataType,
                    IsList = false
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

