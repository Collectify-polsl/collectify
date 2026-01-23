using Collectify.App;
using Collectify.App.Commands;
using Collectify.Model.Entities;
using Collectify.Model.Enums;
using Collectify.Model.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Collectify.App.ViewModels;

// Coordinates the creation of new collections by linking user-defined names to existing metadata templates.
public class NewCollectionViewModel : INotifyPropertyChanged
{
    private readonly ICollectionService _collectionService;
    private readonly ITemplateService _templateService;

    public Action? CloseAction { get; set; }

    private string _collectionName = string.Empty;
    public string CollectionName
    {
        get => _collectionName;
        set
        {
            _collectionName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ErrorMessage));
            (CreateCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    private string _collectionDescription = string.Empty;
    public string CollectionDescription
    {
        get => _collectionDescription;
        set
        {
            _collectionDescription = value;
            OnPropertyChanged();
        }
    }

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
    public Template? SelectedTemplate
    {
        get => _selectedTemplate;
        set
        {
            _selectedTemplate = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ErrorMessage));
            LoadTemplateFields();
            (CreateCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    public string? ErrorMessage => CanSave() ? null : "Please fill in all fields.";

    public ObservableCollection<ColumnItem> DisplayedColumns { get; } = new();

    public ICommand CreateCommand { get; }

    public NewCollectionViewModel(
        ICollectionService collectionService,
        ITemplateService templateService)
    {
        _collectionService = collectionService;
        _templateService = templateService;

        CreateCommand = new AsyncRelayCommand(SaveAsync, CanSave);

        LoadTemplates();
    }

    // Fetches the list of available templates to populate the selection dropdown.
    private async void LoadTemplates()
    {
        var templates = await _templateService.GetAllTemplatesAsync();
        TemplateList.Clear();
        foreach (var t in templates)
            TemplateList.Add(t);
    }

    // Retrieves and displays the specific fields associated with the chosen template to preview the collection structure.
    private async void LoadTemplateFields()
    {
        DisplayedColumns.Clear();
        if (SelectedTemplate == null) return;

        try
        {
            var fullTemplate = await _templateService.GetTemplateAsync(
                SelectedTemplate.Id,
                includeFields: true);

            foreach (var field in fullTemplate.Fields)
            {
                DisplayedColumns.Add(new ColumnItem
                {
                    Name = field.Name,
                    DataType = field.FieldType,
                    IsList = field.IsList,
                    InitialIsList = field.IsList
                });
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading template: {ex.Message}";
            StatusType = StatusMessageType.Error;
        }
    }

    // Ensures that both a name and a template are provided before allowing collection creation.
    private bool CanSave()
        => !string.IsNullOrWhiteSpace(CollectionName)
           && SelectedTemplate != null;

    // Persists the new collection to the database and closes the creation dialog upon success.
    private async Task SaveAsync()
    {
        try
        {
            StatusMessage = string.Empty;
            await _collectionService.CreateCollectionAsync(
                SelectedTemplate!.Id,
                CollectionName,
                CollectionDescription);

            CloseAction?.Invoke();
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

// Represents a single field definition within a template for display and editing in the UI.
public class ColumnItem : INotifyPropertyChanged
{
    private int? _id;
    private string _name = string.Empty;
    private FieldType _dataType;
    private bool _isList;
    private bool _initialIsList;

    public int? Id
    {
        get => _id;
        set => SetField(ref _id, value);
    }

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    public FieldType DataType
    {
        get => _dataType;
        set => SetField(ref _dataType, value);
    }

    public bool IsList
    {
        get => _isList;
        set
        {
            if (SetField(ref _isList, value))
                OnPropertyChanged(nameof(HasListChange));
        }
    }

    public bool InitialIsList
    {
        get => _initialIsList;
        set
        {
            if (SetField(ref _initialIsList, value))
                OnPropertyChanged(nameof(HasListChange));
        }
    }

    // Indicates if the "IsList" property has been modified from its original database state.
    public bool HasListChange => Id.HasValue && IsList != InitialIsList;

    public event PropertyChangedEventHandler? PropertyChanged;

    // Updates a field value and triggers a property change notification if the value has changed.
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged(string? name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}