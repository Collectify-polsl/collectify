using Collectify.App.Commands;
using Collectify.App;
using Collectify.Model.Enums;
using Collectify.Model.Interfaces;
using Collectify.Model.Entities;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Collectify.App.ViewModels;

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

    private async void LoadTemplates()
    {
        var templates = await _templateService.GetAllTemplatesAsync();
        TemplateList.Clear();
        foreach (var t in templates)
            TemplateList.Add(t);
    }

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
                    DataType = field.FieldType
                });
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading template: {ex.Message}";
            StatusType = StatusMessageType.Error;
        }
    }

    private bool CanSave()
        => !string.IsNullOrWhiteSpace(CollectionName)
           && SelectedTemplate != null;

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

public class ColumnItem
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public FieldType DataType { get; set; }
}