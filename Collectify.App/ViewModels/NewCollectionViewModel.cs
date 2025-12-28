using Collectify.App.Commands;
using Collectify.Model.Enums;
using Collectify.Model.InputModels;
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

    public ObservableCollection<string> Modes { get; } = new() { "Dodawanie własnych pól", "Wybierz gotowy zestaw" };
    private string _selectedMode = "Dodawanie własnych pól";
    public string SelectedMode
    {
        get => _selectedMode;
        set
        {
            if (_selectedMode != value)
            {
                _selectedMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsCustomMode));
                OnPropertyChanged(nameof(IsTemplateMode));
                UpdateDisplayedColumns();
            }
        }
    }

    public bool IsCustomMode => SelectedMode == "Dodawanie własnych pól";
    public bool IsTemplateMode => SelectedMode == "Wybierz gotowy zestaw";

    private string _collectionName = string.Empty;
    public string CollectionName
    {
        get => _collectionName;
        set { _collectionName = value; OnPropertyChanged(); (SaveCollectionCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged(); }
    }

    private string _collectionDescription = string.Empty;
    public string CollectionDescription
    {
        get => _collectionDescription;
        set { _collectionDescription = value; OnPropertyChanged(); }
    }

    private string _newColumnName = string.Empty;
    public string NewColumnName
    {
        get => _newColumnName;
        set { _newColumnName = value; OnPropertyChanged(); (AddColumnCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
    }

    public IEnumerable<FieldType> DataTypeList { get; } = new[] { FieldType.Text, FieldType.Integer, FieldType.Date };
    private FieldType _selectedNewColumnType = FieldType.Text;
    public FieldType SelectedNewColumnType
    {
        get => _selectedNewColumnType;
        set { _selectedNewColumnType = value; OnPropertyChanged(); (SaveCollectionCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged(); }
    }

    public ObservableCollection<ColumnItem> AddedColumns { get; } = new();
    public ObservableCollection<Template> TemplateList { get; } = new();

    private Template? _selectedTemplate;
    public Template? SelectedTemplate
    {
        get => _selectedTemplate;
        set
        {
            if (_selectedTemplate != value)
            {
                _selectedTemplate = value;
                OnPropertyChanged();

                if (_selectedTemplate != null)
                {
                    LoadFullTemplate(_selectedTemplate);
                }
                else
                {
                    DisplayedColumns.Clear();
                }
            }
        }
    }

    private async void LoadFullTemplate(Template template)
    {
        try
        {
            var fullTemplate = await _templateService.GetTemplateAsync(template.Id, includeFields: true);
            if (fullTemplate != null)
            {
                template.Fields = fullTemplate.Fields;
                UpdateDisplayedColumns();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Błąd przy ładowaniu szablonu: {ex.Message}");
        }
    }

    private void UpdateDisplayedColumns()
    {
        DisplayedColumns.Clear();
        if (IsCustomMode)
        {
            foreach (var c in AddedColumns)
                DisplayedColumns.Add(c);
        }
        else if (IsTemplateMode && SelectedTemplate != null)
        {
            foreach (var f in SelectedTemplate.Fields)
                DisplayedColumns.Add(new ColumnItem { Name = f.Name, DataType = f.FieldType });
        }
    }

    public ObservableCollection<ColumnItem> DisplayedColumns { get; } = new();

    public ICommand AddColumnCommand { get; }
    public ICommand RemoveColumnCommand { get; }
    public ICommand SaveCollectionCommand { get; }
    public ICommand SaveCreatedTemplateCommand { get; }

    public NewCollectionViewModel(ICollectionService collectionService, ITemplateService templateService)
    {
        _collectionService = collectionService;
        _templateService = templateService;

        AddColumnCommand = new RelayCommand(AddColumn, CanAddColumn);
        RemoveColumnCommand = new RelayCommand<ColumnItem>(RemoveColumn);
        SaveCollectionCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        SaveCreatedTemplateCommand = new AsyncRelayCommand(SaveTemplateAsync);

        AddedColumns.CollectionChanged += (s, e) => { UpdateDisplayedColumns(); (SaveCollectionCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged(); };
        LoadTemplates();
    }


    private bool CanAddColumn() => !string.IsNullOrWhiteSpace(NewColumnName) && !AddedColumns.Any(c => c.Name.Trim().Equals(NewColumnName.Trim(), System.StringComparison.OrdinalIgnoreCase));
    private void AddColumn() { AddedColumns.Add(new ColumnItem { Name = NewColumnName, DataType = SelectedNewColumnType }); NewColumnName = string.Empty; }
    private void RemoveColumn(ColumnItem item) { if (item != null && AddedColumns.Contains(item)) AddedColumns.Remove(item); }
    private bool CanSave() => !string.IsNullOrWhiteSpace(CollectionName) && (IsCustomMode ? AddedColumns.Any() : SelectedTemplate != null);

    private async Task SaveAsync()
    {
        try
        {
            Template template;

            if (IsCustomMode)
            {
                var fieldInputs = AddedColumns.Select(c => new TemplateFieldDefinitionInput { Name = c.Name, FieldType = c.DataType, IsList = false }).ToList();
                template = await _templateService.CreateTemplateAsync($"{CollectionName}", fieldInputs);
            }
            else
            {
                if (SelectedTemplate == null) { MessageBox.Show("Wybierz szablon!"); return; }
                template = await _templateService.GetTemplateAsync(SelectedTemplate.Id, includeFields: true);
                if (template == null) { MessageBox.Show("Wybrany szablon nie istnieje!"); return; }
            }

            await _collectionService.CreateCollectionAsync(template.Id, CollectionName, CollectionDescription);
            MessageBox.Show("Kolekcja została utworzona!", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
            CloseAction?.Invoke();
        }
        catch (Exception ex) { MessageBox.Show($"Błąd: {ex.Message}"); }
    }
    private string _newTemplateName = string.Empty;
    public string NewTemplateName
    {
        get => _newTemplateName;
        set { _newTemplateName = value; OnPropertyChanged(); }
    }

    private async Task SaveTemplateAsync()
    {
        if (!AddedColumns.Any()) { MessageBox.Show("Dodaj kolumny, aby zapisać szablon."); return; }

        var fieldInputs = AddedColumns
            .Select(c => new TemplateFieldDefinitionInput { Name = c.Name, FieldType = c.DataType, IsList = false })
            .ToList();

        await _templateService.CreateTemplateAsync(NewTemplateName, fieldInputs);

        MessageBox.Show("Szablon zapisany!");

        // Zachowaj bieżące kolumny
        var currentColumns = AddedColumns.ToList();

        LoadTemplates();

        // Przywróć kolumny
        AddedColumns.Clear();
        foreach (var c in currentColumns)
            AddedColumns.Add(c);

        UpdateDisplayedColumns();
    }

    private async void LoadTemplates()
    {
        var templates = await _templateService.GetAllTemplatesAsync();
        TemplateList.Clear();
        foreach (var t in templates) TemplateList.Add(t);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class ColumnItem
{
    public string Name { get; set; } = string.Empty;
    public FieldType DataType { get; set; }
}
