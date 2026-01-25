using Collectify.App.Commands;
using Collectify.Model.Collection;
using Collectify.Model.Entities;
using Collectify.Model.Enums;
using Collectify.Model.InputModels;
using Collectify.Model.Interfaces;
using Collectify.App;
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

/// <summary>
/// View model for creating or editing an item (row) in a collection.
/// </summary>
public class RowWizardViewModel : INotifyPropertyChanged
{
    private readonly Collection _collection;
    private readonly IItemService _itemService;
    private readonly ITemplateService _templateService;
    private readonly ICollectionService _collectionService;
    private readonly Item? _existingItem;

    /// <summary>
    /// Action to close the window.
    /// </summary>
    public Action? CloseAction { get; set; }
    
    /// <summary>
    /// Gets the list of fields to be filled for the item.
    /// </summary>
    public ObservableCollection<FieldInputViewModel> Fields { get; } = new();
    
    /// <summary>
    /// Gets the list of items in the current collection for linking (Previous/Next).
    /// </summary>
    public ObservableCollection<ItemSelectionViewModel> CurrentCollectionItems { get; } = new();
    
    private List<Item> _rawCollectionItems = new();

    private string _previousItemDisplay = "(None)";
    public string PreviousItemDisplay
    {
        get => _previousItemDisplay;
        set { _previousItemDisplay = value; OnPropertyChanged(); }
    }

    private string _nextItemDisplay = "(None)";
    public string NextItemDisplay
    {
        get => _nextItemDisplay;
        set { _nextItemDisplay = value; OnPropertyChanged(); }
    }

    private int? _selectedPreviousItemId;
    public int? SelectedPreviousItemId
    {
        get => _selectedPreviousItemId;
        set { _selectedPreviousItemId = value; OnPropertyChanged(); }
    }

    private int? _selectedNextItemId;
    public int? SelectedNextItemId
    {
        get => _selectedNextItemId;
        set { _selectedNextItemId = value; OnPropertyChanged(); }
    }

    public Dictionary<string, List<Item>> ItemsByCollectionMap { get; } = new();

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

    public bool? DialogResult { get; private set; }

    public ICommand SubmitRowCommand { get; }
    public ICommand UploadImageCommand { get; }
    public ICommand OpenReferencePickerCommand { get; }
    public ICommand OpenPreviousPickerCommand { get; }
    public ICommand OpenNextPickerCommand { get; }

    public RowWizardViewModel(Collection collection, IItemService itemService,
                              ITemplateService templateService, ICollectionService collectionService, Item? existingItem = null)
    {
        _collection = collection;
        _itemService = itemService;
        _templateService = templateService;
        _collectionService = collectionService;
        _existingItem = existingItem;

        SubmitRowCommand = new AsyncRelayCommand(SubmitRowAsync, CanSubmit);
        UploadImageCommand = new RelayCommand<FieldInputViewModel>(UploadImage);
        OpenReferencePickerCommand = new RelayCommand<FieldInputViewModel>(OpenReferencePicker);
        OpenPreviousPickerCommand = new RelayCommand(OpenPreviousPicker);
        OpenNextPickerCommand = new RelayCommand(OpenNextPicker);

        InitializeAsync();
    }

    private bool CanSubmit()
    {
        return Fields.All(f => 
            f.Value != null && 
            (!(f.Value is string s) || !string.IsNullOrWhiteSpace(s)) &&
            (!(f.Value is byte[] bytes) || bytes.Length > 0));
    }

    private async void InitializeAsync()
    {
        try
        {
            var template = await _templateService.GetTemplateAsync(_collection.TemplateId, includeFields: true);
            if (template == null) return;

            // Load items for Next/Previous selection
            var currentItems = await _itemService.GetItemsForCollectionAsync(_collection.Id);
            _rawCollectionItems = currentItems.Where(i => _existingItem == null || i.Id != _existingItem.Id).ToList();
            
            CurrentCollectionItems.Clear();
            CurrentCollectionItems.Add(new ItemSelectionViewModel { Id = -1, DisplayText = "(None)" });

            foreach (var item in _rawCollectionItems)
            {
                CurrentCollectionItems.Add(new ItemSelectionViewModel 
                { 
                    Id = item.Id, 
                    DisplayText = GetItemDisplayText(item) 
                });
            }

            if (_existingItem != null)
            {
                SelectedPreviousItemId = _existingItem.PreviousItemId ?? -1;
                SelectedNextItemId = _existingItem.NextItemId ?? -1;
                
                if (_existingItem.PreviousItemId.HasValue)
                {
                    var prev = currentItems.FirstOrDefault(i => i.Id == _existingItem.PreviousItemId.Value);
                    PreviousItemDisplay = prev != null ? GetItemDisplayText(prev) : $"Item #{_existingItem.PreviousItemId}";
                }
                
                if (_existingItem.NextItemId.HasValue)
                {
                    var next = currentItems.FirstOrDefault(i => i.Id == _existingItem.NextItemId.Value);
                    NextItemDisplay = next != null ? GetItemDisplayText(next) : $"Item #{_existingItem.NextItemId}";
                }
            }
            else
            {
                SelectedPreviousItemId = -1;
                SelectedNextItemId = -1;
            }

            // Load Lookups for References
            var collections = await _collectionService.GetCollectionsAsync();
            ItemsByCollectionMap.Clear();
            
            var templateCache = new Dictionary<int, Template>();

            foreach (var col in collections)
            {
                if (!templateCache.ContainsKey(col.TemplateId))
                {
                    var t = await _templateService.GetTemplateAsync(col.TemplateId, includeFields: true);
                    if (t != null) templateCache[col.TemplateId] = t;
                }

                var items = await _itemService.GetItemsForCollectionAsync(col.Id);
                var itemsList = items.ToList();

                if (templateCache.TryGetValue(col.TemplateId, out var tmpl))
                {
                    var fieldMap = tmpl.Fields.ToDictionary(f => f.Id);
                    foreach (var itm in itemsList)
                    {
                        foreach (var fv in itm.FieldValues)
                        {
                            if (fieldMap.TryGetValue(fv.FieldDefinitionId, out var def))
                            {
                                fv.FieldDefinition = def;
                            }
                        }
                    }
                }

                ItemsByCollectionMap[col.Name] = itemsList;
            }

            Fields.Clear();
            foreach (var field in template.Fields)
            {
                var inputVM = new FieldInputViewModel
                {
                    FieldId = field.Id,
                    FieldName = field.Name,
                    FieldType = field.FieldType
                };

                if (_existingItem != null)
                {
                    var valObj = _existingItem.FieldValues.FirstOrDefault(v => v.FieldDefinitionId == field.Id);
                    if (valObj != null)
                    {
                        switch (field.FieldType)
                        {
                            case FieldType.Integer: inputVM.Value = valObj.IntValue; break;
                            case FieldType.Decimal: inputVM.Value = valObj.DecimalValue; break;
                            case FieldType.Date: inputVM.Value = valObj.DateValue; break;
                            case FieldType.Image: inputVM.Value = valObj.ImageValue; break;
                            case FieldType.ItemReference: 
                                inputVM.Value = valObj.RelatedItemId;
                                if (valObj.RelatedItemId.HasValue)
                                {
                                    var relatedItem = ItemsByCollectionMap.Values.SelectMany(x => x).FirstOrDefault(i => i.Id == valObj.RelatedItemId.Value);
                                    if (relatedItem != null)
                                    {
                                        var firstText = relatedItem.FieldValues.FirstOrDefault(v => !string.IsNullOrEmpty(v.TextValue))?.TextValue;
                                        inputVM.DisplayValue = firstText ?? $"Item #{relatedItem.Id}";
                                    }
                                }
                                break;
                            default: inputVM.Value = valObj.TextValue; break;
                        }
                    }
                }

                inputVM.PropertyChanged += (s, e) => 
                {
                    if (e.PropertyName == nameof(FieldInputViewModel.Value))
                    {
                        (SubmitRowCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                    }
                };

                Fields.Add(inputVM);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error initializing form: {ex.Message}";
            StatusType = StatusMessageType.Error;
        }
    }

    private void OpenPreviousPicker()
    {
        var currentSelected = SelectedPreviousItemId == -1 ? null : SelectedPreviousItemId;
        var picker = new LocalLinkPickerWindow(_rawCollectionItems, currentSelected);
        picker.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);    

        if (picker.ShowDialog() == true)
        {
            SelectedPreviousItemId = picker.SelectedItemId ?? -1;
            if (picker.SelectedItemId.HasValue)
            {
                var item = _rawCollectionItems.FirstOrDefault(i => i.Id == picker.SelectedItemId.Value);
                PreviousItemDisplay = item != null ? GetItemDisplayText(item) : $"Item #{picker.SelectedItemId}";
            }
            else
            {
                PreviousItemDisplay = "(None)";
            }
        }
    }

    private void OpenNextPicker()
    {
        var currentSelected = SelectedNextItemId == -1 ? null : SelectedNextItemId;
        var picker = new LocalLinkPickerWindow(_rawCollectionItems, currentSelected);
        picker.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);    

        if (picker.ShowDialog() == true)
        {
            SelectedNextItemId = picker.SelectedItemId ?? -1;
            if (picker.SelectedItemId.HasValue)
            {
                var item = _rawCollectionItems.FirstOrDefault(i => i.Id == picker.SelectedItemId.Value);
                NextItemDisplay = item != null ? GetItemDisplayText(item) : $"Item #{picker.SelectedItemId}";
            }
            else
            {
                NextItemDisplay = "(None)";
            }
        }
    }

    private void OpenReferencePicker(FieldInputViewModel? field)
    {
        if (field == null) return;

        var picker = new ReferencePickerWindow(ItemsByCollectionMap);
        picker.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);    

        if (picker.ShowDialog() == true)
        {
            field.Value = picker.SelectedItemId;

            if (picker.SelectedItemId.HasValue)
            {
                var selectedItem = ItemsByCollectionMap.Values.SelectMany(x => x).FirstOrDefault(i => i.Id == picker.SelectedItemId.Value);
                if (selectedItem != null)
                {
                    var firstText = selectedItem.FieldValues.FirstOrDefault(v => !string.IsNullOrEmpty(v.TextValue))?.TextValue;
                    field.DisplayValue = firstText ?? $"Item #{selectedItem.Id}";
                }
            }
        }
    }

    private void UploadImage(FieldInputViewModel? field)
    {
        if (field == null) return;

        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp",
            Title = "Select Item Image"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            try
            {
                byte[] imageBytes = System.IO.File.ReadAllBytes(openFileDialog.FileName);
                field.Value = imageBytes;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to load image: {ex.Message}";
                StatusType = StatusMessageType.Error;
            }
        }
    }

    private async Task SubmitRowAsync()
    {
        try
        {
            StatusMessage = string.Empty;
            
            bool allFilled = Fields.All(f => 
                f.Value != null && 
                (!(f.Value is string s) || !string.IsNullOrWhiteSpace(s)) &&
                (!(f.Value is byte[] bytes) || bytes.Length > 0));

            if (!allFilled)
            {
                StatusMessage = "Please fill in all fields to save the item.";
                StatusType = StatusMessageType.Error;
                return;
            }

            var inputs = new List<NewItemFieldValueInput>();

            foreach (var field in Fields)
            {
                if (field.Value == null) continue;
                if (field.Value is string s && string.IsNullOrWhiteSpace(s)) continue;

                var input = new NewItemFieldValueInput { FieldDefinitionId = field.FieldId };
                
                try 
                {
                    switch (field.FieldType)
                    {
                        case FieldType.Integer: 
                            if (!int.TryParse(field.Value.ToString(), out int intVal))
                                throw new Exception($"Field '{field.Name}' must be a valid whole number.");
                            input.IntValue = intVal; 
                            break;
                        case FieldType.Decimal: 
                            if (!decimal.TryParse(field.Value.ToString(), out decimal decVal))
                                throw new Exception($"Field '{field.Name}' must be a valid decimal number.");
                            input.DecimalValue = decVal; 
                            break;
                        case FieldType.Date: 
                            input.DateValue = (DateTime)field.Value; 
                            break;
                        case FieldType.Image: 
                            input.ImageValue = (byte[])field.Value; 
                            break;
                        case FieldType.ItemReference: 
                            input.RelatedItemId = Convert.ToInt32(field.Value); 
                            break;
                        default: 
                            input.TextValue = field.Value.ToString(); 
                            break;
                    }
                    inputs.Add(input);
                }
                catch (Exception ex)
                {
                    StatusMessage = ex.Message;
                    StatusType = StatusMessageType.Error;
                    return;
                }
            }

            int? prevId = SelectedPreviousItemId == -1 ? null : SelectedPreviousItemId;
            int? nextId = SelectedNextItemId == -1 ? null : SelectedNextItemId;

            if (prevId.HasValue && nextId.HasValue && prevId == nextId)
            {
                StatusMessage = "Previous and Next items cannot be the same.";
                StatusType = StatusMessageType.Error;
                return;
            }

            if (_existingItem != null)
            {
                 await _itemService.UpdateItemAsync(_existingItem.Id, inputs, prevId, nextId);
                 DialogResult = true; 
            }
            else
            {
                await _itemService.CreateItemAsync(_collection.Id, inputs, prevId, nextId);
                DialogResult = true;
            }
            
            CloseAction?.Invoke();
        }
        catch (Exception ex) 
        { 
            StatusMessage = $"Error saving item: {ex.Message}";
            StatusType = StatusMessageType.Error;
        }
    }

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

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// Represents a single field input in the row wizard, handling value binding and display.
/// </summary>
public class FieldInputViewModel : INotifyPropertyChanged
{
    public int FieldId { get; set; }
    public string Name => FieldName; // Alias for binding
    public string FieldName { get; set; } = string.Empty;
    public FieldType FieldType { get; set; }

    private object? _value;
    public object? Value
    {
        get => _value;
        set { _value = value; OnPropertyChanged(); }
    }

    private string _displayValue = string.Empty;
    public string DisplayValue
    {
        get => _displayValue;
        set { _displayValue = value; OnPropertyChanged(); }
    }

    public bool IsText => FieldType == FieldType.Text || FieldType == FieldType.Integer || FieldType == FieldType.Decimal; 
    public bool IsDate => FieldType == FieldType.Date;
    public bool IsImage => FieldType == FieldType.Image;
    public bool IsReference => FieldType == FieldType.ItemReference;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// Represents an item in a selection list (e.g., for Previous/Next item linking).
/// </summary>
public class ItemSelectionViewModel
{
    public int Id { get; set; }
    public string DisplayText { get; set; } = string.Empty;
}