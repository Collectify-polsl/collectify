using Collectify.App.Commands;
using Collectify.Model.Collection;
using Collectify.Model.Entities;
using Collectify.Model.Enums;
using Collectify.Model.Interfaces;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Collectify.App.ViewModels;

public class SingleCollectionViewModel : INotifyPropertyChanged
{
    private readonly Collection _currentCollection;
    private readonly IItemService _itemService;
    private readonly ITemplateService _templateService;
    private readonly ICollectionService _collectionService;
    private readonly Func<Collection, Window> _rowWizardFactory;

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
            _selectedCollectionItem = value;
            OnPropertyChanged();

            if (value != null && value.Id != _currentCollection.Id)
            {
                SwitchCollectionAction?.Invoke(value.Id);
            }
        }
    }
    private int? _itemToHighlight;

    // Właściwość podpięta pod SelectedItem w XAML
    private DataRowView? _selectedRow;
    public DataRowView? SelectedRow
    {
        get => _selectedRow;
        set
        {
            _selectedRow = value;
            OnPropertyChanged();
        }
    }

    public ICommand AddNewElementCommand { get; }
    public ICommand ReturnCollectionsViewCommand { get; }
    public ICommand DeleteCollectionCommand { get; }
    // Dodaj te pola i właściwości do klasy
    public ICommand NavigateToReferencedItemCommand { get; }
    public ICommand OpenFullImageCommand { get; }


    // Metoda obsługująca nawigację
    private async void NavigateToReferencedItem(object? idObj)
    {   /*
        if (idObj is int itemId)
        {
            // Pobierz informacje o przedmiocie, aby wiedzieć do której kolekcji należy
            var targetItem = await _itemService.GetItemByIdAsync(itemId);
            if (targetItem != null)
            {
                // Wywołaj akcję zmiany kolekcji zdefiniowaną w View
                SwitchCollectionAction?.Invoke(targetItem.CollectionId);

                // Opcjonalnie: Tutaj można dodać logikę podświetlania wiersza po załadowaniu
                MessageBox.Show($"Navigating to item in collection ID: {targetItem.CollectionId}");
            }
        }*/
    }
    public Action<int>? SwitchCollectionAction { get; set; }
    public Action? NavigateBackAction { get; set; }

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
        DeleteCollectionCommand = new RelayCommand(DeleteCollection);
        NavigateToReferencedItemCommand = new RelayCommand<object>(NavigateToReferencedItem);
        OpenFullImageCommand = new RelayCommand<object>(OpenFullImage);

        LoadDataAsync();
        LoadCollectionListAsync();
    }

    private void OpenNewElementCreator()
    {
        var window = _rowWizardFactory(_currentCollection);
        window.ShowDialog();
        LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var template = await _templateService.GetTemplateAsync(_currentCollection.TemplateId, includeFields: true);
            if (template == null) return;

            var items = await _itemService.GetItemsForCollectionAsync(_currentCollection.Id);

            DataTable table = new DataTable();

            // --- ZMIANA 1: Dodajemy techniczną kolumnę Id ---
            table.Columns.Add("Id", typeof(int));

            var sortedFields = template.Fields.OrderBy(f => f.Id).ToList();

            foreach (var field in sortedFields)
            {
                table.Columns.Add(field.Name, GetTypeForField(field.FieldType));
            }

            foreach (var item in items)
            {
                DataRow row = table.NewRow();

                // --- ZMIANA 2: Przypisujemy Id przedmiotu do wiersza ---
                row["Id"] = item.Id;

                foreach (var field in sortedFields)
                {
                    var valObj = item.FieldValues.FirstOrDefault(v => v.FieldDefinitionId == field.Id);
                    row[field.Name] = GetRawValue(valObj, field.FieldType) ?? DBNull.Value;
                }

                table.Rows.Add(row);
            }

            DynamicTable = table.DefaultView;

            // --- ZMIANA 3: Logika szukania i podświetlania wiersza ---
            if (_itemToHighlight.HasValue)
            {
                // Przeszukujemy nowo załadowaną tabelę
                foreach (DataRowView rowView in DynamicTable)
                {
                    if (Convert.ToInt32(rowView["Id"]) == _itemToHighlight.Value)
                    {
                        // Ustawiamy SelectedRow – to spowoduje podświetlenie w DataGrid (przez Binding)
                        SelectedRow = rowView;
                        break;
                    }
                }
                // Czyścimy ID, żeby przy kolejnym (zwykłym) wejściu nie podświetlało nic starego
                _itemToHighlight = null;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading data: {ex.Message}");
        }
    }

    private async Task LoadCollectionListAsync()
    {
        try
        {
            var allCollections = await _collectionService.GetCollectionsAsync();

            CollectionList.Clear();
            foreach (var collection in allCollections.OrderBy(c => c.Name))
            {
                CollectionList.Add(new CollectionDisplayItem(collection.Id, collection.Name));
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading collection list: {ex.Message}");
        }
    }

    private async void DeleteCollection()
    {
        var result = MessageBox.Show(
            $"Are you sure you want to delete the collection \"{_currentCollection.Name}\"?\nThis operation cannot be undone.",
            "Confirm deletion",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            await _collectionService.DeleteCollectionAsync(_currentCollection.Id);

            MessageBox.Show("Collection deleted successfully.", "Deleted", MessageBoxButton.OK, MessageBoxImage.Information);
            NavigateBackAction?.Invoke();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error deleting collection: {ex.Message}");
        }
    }

    private Type GetTypeForField(FieldType type) => type switch
    {
        FieldType.Integer => typeof(int),
        FieldType.Decimal => typeof(decimal),
        FieldType.Date => typeof(string),
        FieldType.ItemReference => typeof(int),
        FieldType.Image => typeof(byte[]),
        FieldType.Text => typeof(string),
        _ => typeof(string)
    };

    private object? GetRawValue(FieldValue? value, FieldType type)
    {
        if (value == null) return null;

        return type switch
        {
            FieldType.Integer => value.IntValue,
            FieldType.Decimal => value.DecimalValue,
            FieldType.Date => value.DateValue?.ToString("dd/MM/yyyy"),
            FieldType.ItemReference => value.RelatedItemId,
            FieldType.Image => value.ImageValue,
            FieldType.Text => value.TextValue,
            _ => value.TextValue
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    private void OpenFullImage(object? parameter)
    {
        byte[]? imageData = null;

        // Przypadek 1: Parametr to bezpośrednio bajty
        if (parameter is byte[] bytes)
        {
            imageData = bytes;
        }
        // Przypadek 2: Parametr to wiersz (częste w DataGridTemplateColumn)
        else if (parameter is DataRowView rowView)
        {
            // Tutaj musimy wiedzieć, w której kolumnie jest obrazek. 
            // Jeśli nie znamy nazwy, szukamy pierwszej kolumny typu byte[]
            foreach (DataColumn col in rowView.Row.Table.Columns)
            {
                if (col.DataType == typeof(byte[]))
                {
                    imageData = rowView[col.ColumnName] as byte[];
                    break;
                }
            }
        }

        if (imageData == null || imageData.Length == 0) return;

        // Tworzenie okna (Twój kod jest OK)
        var window = new Window
        {
            Title = "View Image",
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            SizeToContent = SizeToContent.WidthAndHeight,
            Background = Brushes.Black
        };

        var imageControl = new System.Windows.Controls.Image
        {
            Source = new BytesToImageConverter().Convert(imageData, typeof(ImageSource), null, System.Globalization.CultureInfo.CurrentCulture) as ImageSource,
            Stretch = Stretch.Uniform,
            MaxWidth = 1000,
            MaxHeight = 800
        };

        imageControl.MouseDown += (s, e) => window.Close();
        window.Content = imageControl;
        window.ShowDialog();
    }
}
