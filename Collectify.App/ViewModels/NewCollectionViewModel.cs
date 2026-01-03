using Collectify.App.Commands;
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
            (SaveCollectionCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
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

    public ObservableCollection<Template> TemplateList { get; } = new();

    private Template? _selectedTemplate;
    public Template? SelectedTemplate
    {
        get => _selectedTemplate;
        set
        {
            _selectedTemplate = value;
            OnPropertyChanged();
            LoadTemplateFields();
            (SaveCollectionCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    public ObservableCollection<ColumnItem> DisplayedColumns { get; } = new();

    public ICommand SaveCollectionCommand { get; }

    public NewCollectionViewModel(
        ICollectionService collectionService,
        ITemplateService templateService)
    {
        _collectionService = collectionService;
        _templateService = templateService;

        SaveCollectionCommand = new AsyncRelayCommand(SaveAsync, CanSave);

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
            MessageBox.Show($"Error loading template: {ex.Message}");
        }
    }

    private bool CanSave()
        => !string.IsNullOrWhiteSpace(CollectionName)
           && SelectedTemplate != null;

    private async Task SaveAsync()
    {
        try
        {
            await _collectionService.CreateCollectionAsync(
                SelectedTemplate!.Id,
                CollectionName,
                CollectionDescription);

            MessageBox.Show(
                "Collection created successfully!",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            CloseAction?.Invoke();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}");
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
