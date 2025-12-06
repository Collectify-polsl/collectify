using Collectify.App.Commands;
using Collectify.Model.Collection;
using Collectify.Model.Entities;
using Collectify.Model.Enums;
using Collectify.Model.Interfaces;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace Collectify.App.ViewModels;

public class SingleCollectionViewModel : INotifyPropertyChanged
{
    private readonly Collection _currentCollection;
    private readonly IItemService _itemService;
    private readonly ITemplateService _templateService;
    private readonly ICollectionService _collectionService;
    private readonly Func<Collection, Window> _rowWizardFactory;

    // Klasa pomocnicza dla ComboBoxa
    public record CollectionDisplayItem(int Id, string Name);
    public ObservableCollection<CollectionDisplayItem> CollectionList { get; } = new();

    private DataView _dynamicTable;
    public DataView DynamicTable
    {
        get => _dynamicTable;
        set { _dynamicTable = value; OnPropertyChanged(); }
    }
    private CollectionDisplayItem? _selectedCollectionItem;
    public CollectionDisplayItem? SelectedCollectionItem
    {
        get => _selectedCollectionItem;
        set
        {
            if (_selectedCollectionItem == value) return;
            _selectedCollectionItem= value;
            OnPropertyChanged();

            // Jeśli wybrano nową kolekcję (różną od aktualnej), wywołaj akcję przełączenia
            if (value != null && value.Id != _currentCollection.Id)
            {
                SwitchCollectionAction?.Invoke(value.Id);
            }
        }
    }

    public ICommand AddNewElementCommand { get; }
    public ICommand ReturnCollectionsViewCommand { get; }
    public Action<int>? SwitchCollectionAction { get; set; }
    public Action? NavigateBackAction { get; set; }

    // --- KONSTRUKTOR ---
    public SingleCollectionViewModel(
        Collection collection,
        IItemService itemService,
        ITemplateService templateService,
        ICollectionService collectionService,
        Func<Collection, Window> rowWizardFactory)
    {
        _currentCollection = collection;
        _itemService = itemService;
        _templateService = templateService;
        _collectionService = collectionService;

        _rowWizardFactory = rowWizardFactory;
        _selectedCollectionItem = new CollectionDisplayItem(_currentCollection.Id, _currentCollection.Name);


        ReturnCollectionsViewCommand = new RelayCommand(() => NavigateBackAction?.Invoke());
        AddNewElementCommand = new RelayCommand(OpenNewElementCreator);

        // Uruchamiamy ładowanie danych przy starcie
        LoadDataAsync();
        LoadCollectionListAsync();
    }

    // --- LOGIKA AKCJI ---
    private void OpenNewElementCreator()
    {
        var window = _rowWizardFactory(_currentCollection);

        // Otwórz okno dialogowe
        window.ShowDialog();

        // Odśwież tabelę po zamknięciu okna kreatora
        LoadDataAsync();
    }

    // --- GŁÓWNA LOGIKA ŁADOWANIA DANYCH ---
    private async Task LoadDataAsync()
    {
        try
        {
            // 1. Pobierz Szablon i Elementy (Wiersze)
            var template = await _templateService.GetTemplateAsync(_currentCollection.TemplateId, includeFields: true);

            if (template == null) return;
            var items = await _itemService.GetItemsForCollectionAsync(_currentCollection.Id);

            // 2. Budowanie struktury tabeli
            DataTable table = new DataTable();
            table.Columns.Add("ID", typeof(int));

            var sortedFields = template.Fields.OrderBy(f => f.Id).ToList();

            // Kolumny Dynamiczne
            foreach (var field in sortedFields)
            {
                Type colType = GetTypeForField(field.FieldType);
                table.Columns.Add(field.Name, colType);
            }

            // 3. Wypełnianie Wierszy
            foreach (var item in items)
            {
                DataRow row = table.NewRow();
                row["ID"] = item.Id;

                foreach (var field in sortedFields)
                {
                    var valObj = item.FieldValues.FirstOrDefault(v => v.FieldDefinitionId == field.Id);
                    row[field.Name] = GetRawValue(valObj, field.FieldType) ?? DBNull.Value;
                }

                table.Rows.Add(row);
            }

            // Przypisanie do widoku
            DynamicTable = table.DefaultView;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Błąd ładowania danych: {ex.Message}");
        }
    }
    private async Task LoadCollectionListAsync()
    {
        try
        {
            var allCollections = await _collectionService.GetCollectionsAsync(); // Używamy serwisu

            CollectionList.Clear();
            foreach (var collection in allCollections.OrderBy(c => c.Name))
            {
                CollectionList.Add(new CollectionDisplayItem(collection.Id, collection.Name));
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Błąd ładowania listy kolekcji: {ex.Message}");
        }
    }
    // --- METODY POMOCNICZE ---

    // Mapuje Enum FieldType na typ .NET
    private Type GetTypeForField(FieldType type)
    {
        return type switch
        {
            FieldType.Integer => typeof(int),
            FieldType.Decimal => typeof(decimal),
            FieldType.Date => typeof(string),
            FieldType.ItemReference => typeof(int),
            FieldType.Image => typeof(byte[]),
            FieldType.Text => typeof(string),
            _ => typeof(string)
        };
    }

    // Wyciąga surową wartość z obiektu FieldValue
    private object? GetRawValue(FieldValue? value, FieldType type)
    {
        if (value == null) return null;

        return type switch
        {
            FieldType.Integer => value.IntValue,
            FieldType.Decimal => value.DecimalValue,
            FieldType.Date => value.DateValue?.ToString("dd'/'MM'/'yyyy"),
            FieldType.ItemReference => value.RelatedItemId,
            FieldType.Image => value.ImageValue,
            FieldType.Text => value.TextValue,
            _ => value.TextValue
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}