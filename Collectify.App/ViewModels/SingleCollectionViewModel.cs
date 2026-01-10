using Collectify.App.Commands;
using Collectify.App.Converters;
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
using System.Windows.Controls;
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

    private bool _isPopupView;
    public bool IsPopupView
    {
        get => _isPopupView;
        set { _isPopupView = value; OnPropertyChanged(); }
    }

    public record CollectionDisplayItem(int Id, string Name);
    public ObservableCollection<CollectionDisplayItem> CollectionList { get; } = new();

    private DataTable? _dataTableWithMetadata;

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

    private string? _selectedFilterColumn;
    public string? SelectedFilterColumn
    {
        get => _selectedFilterColumn;
        set
        {
            _selectedFilterColumn = value;
            OnPropertyChanged();
            ApplyFilter();
        }
    }

    private string? _filterText;
    public string? FilterText
    {
        get => _filterText;
        set
        {
            _filterText = value;
            OnPropertyChanged();
            ApplyFilter();
        }
    }

    private ObservableCollection<string> _availableColumns = new();
    public ObservableCollection<string> AvailableColumns
    {
        get => _availableColumns;
        set
        {
            _availableColumns = value;
            OnPropertyChanged();
        }
    }

    public ICommand AddNewElementCommand { get; }
    public ICommand ReturnCollectionsViewCommand { get; }
    public ICommand DeleteCollectionCommand { get; }
    public ICommand NavigateToReferencedItemCommand { get; }
    public ICommand OpenFullImageCommand { get; }
    public ICommand ClearFilterCommand { get; }

    public record ReferenceValue(int Id);
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
        ClearFilterCommand = new RelayCommand(ClearFilter);

        LoadDataAsync();
        LoadCollectionListAsync();
    }

    private void OpenNewElementCreator()
    {
        var window = _rowWizardFactory(_currentCollection);
        window.ShowDialog();
        LoadDataAsync();
    }

    private async void NavigateToReferencedItem(object? parameter)
    {
        if (parameter is not DataGridCell cell) return;

        if (cell.DataContext is DataRowView rowView)
        {
            string columnName = cell.Column.SortMemberPath;
            object cellValue = rowView[columnName];

            if (cellValue?.GetType().Name == "ReferenceValue")
            {
                dynamic dynamicRef = cellValue;
                int itemId = dynamicRef.Id;

                if (itemId > 0)
                {
                    var targetItem = await _itemService.GetItemAsync(itemId);
                    if (targetItem == null) return;

                    var targetCollection = await _collectionService.GetCollectionAsync(targetItem.CollectionId);
                    if (targetCollection == null) return;

                    var newVm = new SingleCollectionViewModel(
                        targetCollection,
                        _itemService,
                        _templateService,
                        _collectionService,
                        _rowWizardFactory);

                    newVm._itemToHighlight = itemId;
                    newVm.IsPopupView = true;

                    var window = new Window
                    {
                        Title = $"Collection: {targetCollection.Name}",
                        Width = 600,
                        Height = 400,
                        Content = new SingleCollectionView { DataContext = newVm },
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0F2F5"))
                    };

                    window.Show();
                }
            }
        }
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var template = await _templateService.GetTemplateAsync(_currentCollection.TemplateId, includeFields: true);
            if (template == null) return;

            var items = await _itemService.GetItemsForCollectionAsync(_currentCollection.Id);

            DataTable table = new DataTable();
            table.Columns.Add("Id", typeof(int));

            var sortedFields = template.Fields.OrderBy(f => f.Id).ToList();

            foreach (var field in sortedFields)
            {
                table.Columns.Add(field.Name, GetTypeForField(field.FieldType));

                if (field.FieldType == FieldType.ItemReference)
                {
                    table.Columns.Add($"{field.Name}_CollectionName", typeof(string));
                }
            }

            foreach (var item in items)
            {
                DataRow row = table.NewRow();
                row["Id"] = item.Id;

                foreach (var field in sortedFields)
                {
                    var valObj = item.FieldValues.FirstOrDefault(v => v.FieldDefinitionId == field.Id);
                    row[field.Name] = GetRawValue(valObj, field.FieldType) ?? DBNull.Value;

                    if (field.FieldType == FieldType.ItemReference && valObj?.RelatedItemId.HasValue == true)
                    {
                        var relatedItem = await _itemService.GetItemAsync(valObj.RelatedItemId.Value);
                        if (relatedItem != null)
                        {
                            var relatedCollection = await _collectionService.GetCollectionAsync(relatedItem.CollectionId);
                            row[$"{field.Name}_CollectionName"] = relatedCollection?.Name ?? string.Empty;
                        }
                        else
                        {
                            row[$"{field.Name}_CollectionName"] = string.Empty;
                        }
                    }
                }

                table.Rows.Add(row);
            }

            _dataTableWithMetadata = table;
            DynamicTable = table.DefaultView;

            AvailableColumns.Clear();
            foreach (DataColumn column in table.Columns)
            {
                if (column.ColumnName != "Id" && !column.ColumnName.EndsWith("_CollectionName") && column.DataType != typeof(byte[]))
                {
                    AvailableColumns.Add(column.ColumnName);
                }
            }

            if (_itemToHighlight.HasValue)
            {
                foreach (DataRowView rowView in DynamicTable)
                {
                    if (Convert.ToInt32(rowView["Id"]) == _itemToHighlight.Value)
                    {
                        SelectedRow = rowView;
                        break;
                    }
                }
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

    private Type GetTypeForField(FieldType type)
    {
        if (type == FieldType.Image) return typeof(byte[]);
        return typeof(object);
    }

    private object? GetRawValue(FieldValue? value, FieldType type)
    {
        if (type == FieldType.Image) return value?.ImageValue;
        if (value == null) return "-";

        object? result = type switch
        {
            FieldType.Integer => value.IntValue,
            FieldType.Decimal => value.DecimalValue,
            FieldType.Date => value.DateValue?.ToString("dd/MM/yyyy"),
            FieldType.ItemReference => value.RelatedItemId.HasValue
                                       ? new ReferenceValue(value.RelatedItemId.Value)
                                       : null,
            FieldType.Text => value.TextValue,
            _ => null
        };

        return result ?? "-";
    }

    private void ApplyFilter()
    {
        if (DynamicTable == null || _dataTableWithMetadata == null) return;

        if (string.IsNullOrWhiteSpace(SelectedFilterColumn) || string.IsNullOrWhiteSpace(FilterText))
        {
            DynamicTable.RowFilter = string.Empty;
            return;
        }

        try
        {
            var column = DynamicTable.Table.Columns[SelectedFilterColumn];
            if (column == null) return;

            var metadataColumnName = $"{SelectedFilterColumn}_CollectionName";
            var hasMetadataColumn = _dataTableWithMetadata.Columns.Contains(metadataColumnName);

            if (column.DataType == typeof(object) && hasMetadataColumn)
            {
                DynamicTable.RowFilter = $"Convert([{metadataColumnName}], 'System.String') LIKE '%{FilterText.Replace("'", "''")}%'";
            }
            else if (column.DataType == typeof(object) || column.DataType == typeof(string))
            {
                DynamicTable.RowFilter = $"Convert([{SelectedFilterColumn}], 'System.String') LIKE '%{FilterText.Replace("'", "''")}%'";
            }
            else if (column.DataType == typeof(int) || column.DataType == typeof(decimal))
            {
                if (decimal.TryParse(FilterText, out decimal numValue))
                {
                    DynamicTable.RowFilter = $"[{SelectedFilterColumn}] = {numValue}";
                }
                else
                {
                    DynamicTable.RowFilter = string.Empty;
                }
            }
            else if (column.DataType == typeof(DateTime))
            {
                DynamicTable.RowFilter = $"Convert([{SelectedFilterColumn}], 'System.String') LIKE '%{FilterText.Replace("'", "''")}%'";
            }
            else if (column.DataType == typeof(byte[]))
            {
                DynamicTable.RowFilter = string.Empty;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Filter error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            DynamicTable.RowFilter = string.Empty;
        }
    }

    private void ClearFilter()
    {
        SelectedFilterColumn = null;
        FilterText = string.Empty;

        if (DynamicTable != null)
        {
            DynamicTable.RowFilter = string.Empty;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private void OpenFullImage(object? parameter)
    {
        byte[]? imageData = null;

        if (parameter is byte[] bytes)
        {
            imageData = bytes;
        }
        else if (parameter is DataRowView rowView)
        {
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