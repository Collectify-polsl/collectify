using Collectify.App.Commands;
using Collectify.App.Converters;
using Collectify.Model.Collection;
using Collectify.Model.Entities;
using Collectify.Model.Enums;
using Collectify.Model.Interfaces;
using Collectify.App;
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
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace Collectify.App.ViewModels;

public class SingleCollectionViewModel : INotifyPropertyChanged
{
    private readonly Collection _currentCollection;
    private readonly IItemService _itemService;
    private readonly ITemplateService _templateService;
    private readonly ICollectionService _collectionService;
    private readonly Func<Collection, Item?, Window> _rowWizardFactory;

    private bool _isPopupView;
    public bool IsPopupView
    {
        get => _isPopupView;
        set { _isPopupView = value; OnPropertyChanged(); }
    }

    public record CollectionDisplayItem(int Id, string Name);
    public ObservableCollection<CollectionDisplayItem> CollectionList { get; } = new();

    private DataTable? _gridDataTable;
    public DataTable? GridDataTable
    {
        get => _gridDataTable;
        set { _gridDataTable = value; OnPropertyChanged(); }
    }

    private ICollectionView _dynamicTable;
    public ICollectionView DynamicTable
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
            (EditItemCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (DeleteItemCommand as AsyncRelayCommand)?.
            RaiseCanExecuteChanged();
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
    public ICommand EditCollectionCommand { get; }
    public ICommand EditItemCommand { get; }
    public ICommand DeleteItemCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ShowReferencingItemsCommand { get; }

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

    private CancellationTokenSource? _statusCts;

    private void ShowStatus(string message, StatusMessageType type, int durationMilliseconds = 5000)
    {
        _statusCts?.Cancel();
        _statusCts = new CancellationTokenSource();
        var token = _statusCts.Token;

        StatusMessage = message;
        StatusType = type;

        Task.Delay(durationMilliseconds, token).ContinueWith(t =>
        {
            if (!t.IsCanceled)
            {
                Application.Current.Dispatcher.Invoke(() => StatusMessage = string.Empty);
            }
        });
    }

    public record ReferenceValue(int Id);
    public Action<int>? SwitchCollectionAction { get; set; }
    public Action? NavigateBackAction { get; set; }

    public SingleCollectionViewModel(
        Collection collection,
        IItemService itemService,
        ITemplateService templateService,
        ICollectionService collectionService,
        Func<Collection, Item?, Window> rowWizardFactory)
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
        EditCollectionCommand = new AsyncRelayCommand(EditCollectionAsync);
        EditItemCommand = new RelayCommand(EditSelectedItem, () => SelectedRow != null);
        DeleteItemCommand = new AsyncRelayCommand(DeleteSelectedItemAsync, () => SelectedRow != null);
        RefreshCommand = new AsyncRelayCommand(LoadDataAsync);

        ShowReferencingItemsCommand = new RelayCommand<object>(ShowReferencingItems);

        LoadDataAsync();
        LoadCollectionListAsync();
    }

    private async void EditSelectedItem()
    {
        if (SelectedRow == null) return;
        int itemId = (int)SelectedRow["Id"];

        StatusMessage = string.Empty;

        var item = await _itemService.GetItemAsync(itemId, includeFieldValues: true);

        if (item != null)
        {
            var window = _rowWizardFactory(_currentCollection, item);
            window.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
            window.Title = "Edit Item";

            window.ShowDialog();
        }
    }

    private async Task DeleteSelectedItemAsync()
    {
        if (SelectedRow == null)
            return;

        int itemId = (int)SelectedRow["Id"];

        var confirm = new ConfirmationWindow(
            $"Are you sure you want to delete item #{itemId}?\nThis operation cannot be undone.",
            "Confirm deletion")
        {
            Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
        };

        if (confirm.ShowDialog() != true)
            return;

        try
        {
            await _itemService.DeleteItemAsync(itemId);
            ShowStatus("Item deleted successfully.", StatusMessageType.Success);
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            ShowStatus($"Error deleting item: {ex.Message}", StatusMessageType.Error);
        }
    }

    public void HighlightItem(int itemId)
    {
        _itemToHighlight = itemId;
        if (DynamicTable != null)
        {
             foreach (DataRowView rowView in DynamicTable)
             {
                 if (Convert.ToInt32(rowView["Id"]) == itemId)
                 {
                     SelectedRow = rowView;
                     break;
                 }
             }
             if (SelectedRow != null) _itemToHighlight = null;
        }
    }

    private async Task EditCollectionAsync()
    {
        var window = new EditCollectionWindow(_currentCollection.Name, _currentCollection.Description);
        window.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        
        if (window.ShowDialog() == true)
        {
            try
            {
                _currentCollection.Description = window.NewDescription;
                                await _collectionService.UpdateCollectionAsync(
                                    _currentCollection.Id, 
                                    _currentCollection.Name, 
                                    _currentCollection.Description);
                
                                ShowStatus("Collection description updated successfully!", StatusMessageType.Success);
                            }
                            catch (Exception ex)
                            {             
                                ShowStatus($"Error updating collection: {ex.Message}", StatusMessageType.Error);
                            }
                        }
                    }
    private void OpenNewElementCreator()
    {
        var window = _rowWizardFactory(_currentCollection, null);
        window.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        if (window.ShowDialog() == true)
        {
            LoadDataAsync();
        }
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

                    newVm.HighlightItem(itemId);
                    newVm.IsPopupView = true;

                    var window = new Window
                    {
                        Title = $"Collection: {targetCollection.Name}",
                        Width = 900,
                        Height = 500,
                        Content = new SingleCollectionView { DataContext = newVm },
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0F2F5"))
                    };

                    newVm.NavigateBackAction = window.Close;

                    window.Show();
                }
            }
        }
    }

    private async Task LoadDataAsync()
    {
        try
        {
            SelectedRow = null;

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

            table.Columns.Add("CreationDate", typeof(DateTime));

            foreach (var item in items)
            {
                DataRow row = table.NewRow();
                row["Id"] = item.Id;
                row["CreationDate"] = item.CreationDate.ToLocalTime();

                foreach (var field in sortedFields)
                {
                    var valObj = item.FieldValues.FirstOrDefault(v => v.FieldDefinitionId == field.Id);
                    row[field.Name] = GetRawValue(valObj, field.FieldType) ?? DBNull.Value;

                    // FIX: for reference fields that are lists, also fill the *_CollectionName column
                    // so the UI doesn't appear "empty" after reload.
                    if (field.FieldType == FieldType.ItemReference)
                    {
                        int? refId = null;

                        // single reference
                        if (valObj?.RelatedItemId is int singleId)
                            refId = singleId;
                        // list reference (pick first for display purposes, consistent with GetRawValue)
                        else if (valObj?.References != null && valObj.References.Count > 0)
                            refId = valObj.References.First().RelatedItemId;

                        if (refId.HasValue)
                        {
                            var relatedItem = await _itemService.GetItemAsync(refId.Value);
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
                        else
                        {
                            row[$"{field.Name}_CollectionName"] = string.Empty;
                        }
                    }
                }

                table.Rows.Add(row);
            }

            GridDataTable = table;

            var list = new List<DataRowView>();
            foreach (DataRowView view in table.DefaultView) list.Add(view);
            DynamicTable = new ListCollectionView(list);

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
            ShowStatus($"Error loading data: {ex.Message}", StatusMessageType.Error);
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
            ShowStatus($"Error loading collection list: {ex.Message}", StatusMessageType.Error);
        }
    }

    private async void DeleteCollection()
    {
        var window = new ConfirmationWindow(
            $"Are you sure you want to delete the collection \"{_currentCollection.Name}\"?\nThis operation cannot be undone.",
            "Confirm deletion");
        window.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);

        if (window.ShowDialog() != true) return;

        try
        {
            await _collectionService.DeleteCollectionAsync(_currentCollection.Id);
            NavigateBackAction?.Invoke();
        }
        catch (Exception ex)
        {
             ShowStatus($"Error deleting collection: {ex.Message}", StatusMessageType.Error);
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

            FieldType.ItemReference =>
                // single reference
                value.RelatedItemId.HasValue
                    ? new ReferenceValue(value.RelatedItemId.Value)
                    // list reference: return a non "-" value so the buttons appear
                    : (value.References != null && value.References.Count > 0)
                        ? new ReferenceValue(value.References.First().RelatedItemId)
                        : null,

            FieldType.Text => value.TextValue,
            _ => null
        };

        return result ?? "-";
    }

    private void ApplyFilter()
    {
        if (DynamicTable == null) return;

        if (string.IsNullOrWhiteSpace(SelectedFilterColumn) || string.IsNullOrWhiteSpace(FilterText))
        {
            DynamicTable.Filter = null;
            StatusMessage = string.Empty;
            return;
        }

        try
        {
            Regex regex = new Regex(FilterText, RegexOptions.IgnoreCase);
            
            StatusMessage = string.Empty;

            DynamicTable.Filter = (obj) =>
            {
                if (obj is DataRowView rowView)
                {
                    var metadataColumnName = $"{SelectedFilterColumn}_CollectionName";
                    if (rowView.Row.Table.Columns.Contains(metadataColumnName))
                    {
                        var val = rowView[metadataColumnName]?.ToString();
                        return val != null && regex.IsMatch(val);
                    }

                    if (!rowView.Row.Table.Columns.Contains(SelectedFilterColumn)) return false;
                    var cellValue = rowView[SelectedFilterColumn];
                    if (cellValue == null || cellValue == DBNull.Value) return false;

                    string textToCheck = cellValue.ToString() ?? "";
                    return regex.IsMatch(textToCheck);
                }
                return false;
            };
            
            StatusMessage = string.Empty;
        }
        catch (ArgumentException)
        {
            ShowStatus("Invalid Regex", StatusMessageType.Error);
        }
        catch (Exception ex)
        {
            ShowStatus($"Filter error: {ex.Message}", StatusMessageType.Error);
        }
    }

    private void ClearFilter()
    {
        SelectedFilterColumn = null;
        FilterText = string.Empty;

        if (DynamicTable != null)
        {
            DynamicTable.Filter = null;
        }
        
        StatusMessage = string.Empty;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private void OpenFullImage(object? parameter)
    {
        try
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

            if (imageData == null || imageData.Length == 0)
            {
                ShowStatus("No image data available for this item.", StatusMessageType.Error);
                return;
            }

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
                MaxWidth = 1200,
                MaxHeight = 900
            };

            window.Content = imageControl;
            window.ShowDialog();
        }
        catch (Exception ex)
        {
             ShowStatus($"Could not open image preview: {ex.Message}", StatusMessageType.Error);
        }
    }

    private async void ShowReferencingItems(object? parameter)
    {
        try
        {
            if (parameter is not DataGridCell cell) return;
            if (cell.DataContext is not DataRowView rowView) return;

            int currentItemId = Convert.ToInt32(rowView["Id"]);

            // Search across ALL collections (references can be cross-collection).
            var allCollections = await _collectionService.GetCollectionsAsync();

            var referencingItems = new List<Item>();

            foreach (var col in allCollections)
            {
                var items = await _itemService.GetItemsForCollectionAsync(col.Id);

                foreach (var item in items)
                {
                    // Ensure FieldValues is loaded (prevents NullReferenceException)
                    var itemWithValues = await _itemService.GetItemAsync(item.Id, includeFieldValues: true);
                    if (itemWithValues?.FieldValues == null) continue;

                    bool referencesCurrent = itemWithValues.FieldValues.Any(v =>
                        (v.RelatedItemId.HasValue && v.RelatedItemId.Value == currentItemId) ||
                        (v.References?.Any(r => r.RelatedItemId == currentItemId) ?? false));

                    if (referencesCurrent)
                        referencingItems.Add(itemWithValues);
                }
            }

            var window = new ReferencePreviewWindow(referencingItems)
            {
                Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive),
                Title = "Referencing Item(s)"
            };

            window.ShowDialog();
        }
        catch (Exception ex)
        {
            ShowStatus($"Could not load referencing items: {ex.Message}", StatusMessageType.Error);
        }
    }
}
