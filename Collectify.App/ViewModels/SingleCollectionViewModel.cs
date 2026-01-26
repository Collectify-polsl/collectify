using Collectify.App.Commands;
using Collectify.App.Converters;
using Collectify.Model.Collection;
using Collectify.Model.Entities;
using Collectify.Model.Enums;
using Collectify.Model.InputModels;
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

/// <summary>
/// View model for displaying and managing a single collection, including its items and filtering.
/// </summary>
public class SingleCollectionViewModel : INotifyPropertyChanged
{
    private readonly Collection _currentCollection;
    private readonly IItemService _itemService;
    private readonly ITemplateService _templateService;
    private readonly ICollectionService _collectionService;
    private readonly Func<Collection, Item?, Window> _rowWizardFactory;

    private bool _isPopupView;
    /// <summary>
    /// Gets or sets a value indicating whether this view is displayed as a popup window.
    /// </summary>
    public bool IsPopupView
    {
        get => _isPopupView;
        set { _isPopupView = value; OnPropertyChanged(); }
    }

    public record CollectionDisplayItem(int Id, string Name);
    
    /// <summary>
    /// Gets the list of available collections for navigation.
    /// </summary>
    public ObservableCollection<CollectionDisplayItem> CollectionList { get; } = new();

    private DataTable? _gridDataTable;
    
    /// <summary>
    /// Gets or sets the data table used for the grid display.
    /// </summary>
    public DataTable? GridDataTable
    {
        get => _gridDataTable;
        set { _gridDataTable = value; OnPropertyChanged(); }
    }

    private ICollectionView _dynamicTable;
    
    /// <summary>
    /// Gets or sets the collection view for the grid, supporting filtering and sorting.
    /// </summary>
    public ICollectionView DynamicTable
    {
        get => _dynamicTable;
        set { _dynamicTable = value; OnPropertyChanged(); }
    }

    private CollectionDisplayItem? _selectedCollectionItem;
    
    /// <summary>
    /// Gets or sets the currently selected collection in the navigation dropdown.
    /// </summary>
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
    
    /// <summary>
    /// Gets or sets the currently selected row in the grid.
    /// </summary>
    public DataRowView? SelectedRow
    {
        get => _selectedRow;
        set
        {
            _selectedRow = value;
            OnPropertyChanged();
            (EditItemCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (DeleteItemCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    private string? _selectedFilterColumn;
    
    /// <summary>
    /// Gets or sets the column selected for filtering.
    /// </summary>
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
    
    /// <summary>
    /// Gets or sets the text used to filter the grid.
    /// </summary>
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
    
    /// <summary>
    /// Gets or sets the list of columns available for filtering.
    /// </summary>
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
    public ICommand RefreshCommand { get; }

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
    public record LocalLinkValue(int Id, string Display);
    
    /// <summary>
    /// Action to execute when switching to another collection.
    /// </summary>
    public Action<int>? SwitchCollectionAction { get; set; }
    
    /// <summary>
    /// Action to execute when navigating back to the main list.
    /// </summary>
    public Action? NavigateBackAction { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SingleCollectionViewModel"/> class.
    /// </summary>
    /// <param name="collection">The collection to display.</param>
    /// <param name="itemService">The service for item operations.</param>
    /// <param name="templateService">The service for template operations.</param>
    /// <param name="collectionService">The service for collection operations.</param>
    /// <param name="rowWizardFactory">Factory to create the item editor window.</param>
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
        DeleteCollectionCommand = new AsyncRelayCommand(DeleteCollection);
        DeleteItemCommand = new RelayCommand(DeleteItem, () => SelectedRow != null);
        NavigateToReferencedItemCommand = new RelayCommand<object>(NavigateToReferencedItem);
        OpenFullImageCommand = new RelayCommand<object>(OpenFullImage);
        ClearFilterCommand = new RelayCommand(ClearFilter);
        EditCollectionCommand = new AsyncRelayCommand(EditCollectionAsync);
        EditItemCommand = new RelayCommand(EditSelectedItem, () => SelectedRow != null);
        RefreshCommand = new AsyncRelayCommand(LoadDataAsync);

        LoadDataAsync();
        LoadCollectionListAsync();
    }

    public ICommand DeleteItemCommand { get; }

    private async void DeleteItem()
    {
        if (SelectedRow == null) return;
        
        int itemId = (int)SelectedRow["Id"];
        
        var window = new ConfirmationWindow(
             "Are you sure you want to delete this item? This operation cannot be undone.",
             "Confirm deletion");
        window.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);

        if (window.ShowDialog() != true) return;

        try
        {
            await _itemService.DeleteItemAsync(itemId);
            LoadDataAsync();
            ShowStatus("Item deleted successfully.", StatusMessageType.Success);
        }
        catch (Exception ex)
        {
            ShowStatus($"Error deleting item: {ex.Message}", StatusMessageType.Error);
        }
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

            if (cellValue is LocalLinkValue localLink)
            {
                 HighlightItem(localLink.Id);
                 return;
            }

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
                        Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Resources/Images/icon.png")),
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

            table.Columns.Add("Creation Date", typeof(DateTime));
            table.Columns.Add("Previous", typeof(object));
            table.Columns.Add("Next", typeof(object));

            foreach (var item in items)
            {
                DataRow row = table.NewRow();
                row["Id"] = item.Id;
                row["Creation Date"] = item.CreationDate.ToLocalTime();
                
                string prevDisplay = "Show";
                string nextDisplay = "Show";

                // Optimized approach: we have all items in 'items' list.
                var prevItem = items.FirstOrDefault(i => i.Id == item.PreviousItemId);
                if (prevItem != null)
                {
                     prevDisplay = GetItemDisplayText(prevItem);
                }

                var nextItem = items.FirstOrDefault(i => i.Id == item.NextItemId);
                if (nextItem != null)
                {
                     nextDisplay = GetItemDisplayText(nextItem);
                }

                row["Previous"] = item.PreviousItemId.HasValue ? new LocalLinkValue(item.PreviousItemId.Value, prevDisplay) : (object)"-";
                row["Next"] = item.NextItemId.HasValue ? new LocalLinkValue(item.NextItemId.Value, nextDisplay) : (object)"-";

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

            if (SelectedFilterColumn == null && AvailableColumns.Any())
            {
                SelectedFilterColumn = AvailableColumns.First();
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

    private async Task DeleteCollection()
    {
        var window = new ConfirmationWindow(
            $"Are you sure you want to delete the collection \"{_currentCollection.Name}\"?\nThis operation cannot be undone.",
            "Confirm deletion");
        window.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);

        if (window.ShowDialog() != true) return;

        try
        {
            // First unlink all items in this collection to avoid circular dependency/constraint issues during cascade delete
            var items = await _itemService.GetItemsForCollectionAsync(_currentCollection.Id);
            foreach (var item in items)
            {
                if (item.PreviousItemId.HasValue || item.NextItemId.HasValue)
                {
                     // Update with nulls
                     await _itemService.UpdateItemAsync(item.Id, new List<NewItemFieldValueInput>(), null, null);
                }
            }

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
            FieldType.Date => value.DateValue?.ToString("yyyy/MM/dd"),
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

    private string GetItemDisplayText(Item item)
    {
        foreach (var val in item.FieldValues)
        {
            if (!string.IsNullOrEmpty(val.TextValue)) return val.TextValue;
            if (val.IntValue.HasValue) return val.IntValue.Value.ToString();
            if (val.DecimalValue.HasValue) return val.DecimalValue.Value.ToString();
            if (val.DateValue.HasValue) return val.DateValue.Value.ToString("yyyy/MM/dd");
        }
        return $"Item #{item.Id}";
    }

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
                Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Resources/Images/icon.png")),
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
}
