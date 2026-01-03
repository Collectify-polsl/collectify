using Collectify.App.Commands;
using Collectify.Model.Collection;
using Collectify.Model.Entities;
using Collectify.Model.Enums;
using Collectify.Model.Interfaces;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
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

    public ICommand AddNewElementCommand { get; }
    public ICommand ReturnCollectionsViewCommand { get; }
    public ICommand DeleteCollectionCommand { get; }

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
            var sortedFields = template.Fields.OrderBy(f => f.Id).ToList();

            foreach (var field in sortedFields)
            {
                table.Columns.Add(field.Name, GetTypeForField(field.FieldType));
            }

            foreach (var item in items)
            {
                DataRow row = table.NewRow();

                foreach (var field in sortedFields)
                {
                    var valObj = item.FieldValues.FirstOrDefault(v => v.FieldDefinitionId == field.Id);
                    row[field.Name] = GetRawValue(valObj, field.FieldType) ?? DBNull.Value;
                }

                table.Rows.Add(row);
            }

            DynamicTable = table.DefaultView;
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
}
