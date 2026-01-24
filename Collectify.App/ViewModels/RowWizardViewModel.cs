using Collectify.App;
using Collectify.App.Commands;
using Collectify.Model.Collection;
using Collectify.Model.Entities;
using Collectify.Model.Enums;
using Collectify.Model.InputModels;
using Collectify.Model.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Collectify.App.ViewModels;

// Facilitates the multi-step process of creating or editing a collection item and its associated metadata fields.
public class RowWizardViewModel : INotifyPropertyChanged
{
    private readonly Collection _collection;
    private readonly IItemService _itemService;
    private readonly ITemplateService _templateService;
    private readonly ICollectionService _collectionService;
    private readonly Item? _existingItem;
    private readonly Dictionary<int, Item> _itemsById = new();

    public Action? CloseAction { get; set; }

    public Action<int>? RequestNavigateToItemId { get; set; }

    public ObservableCollection<FieldInputViewModel> Fields { get; } = new();

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
    public ICommand RemoveReferenceCommand { get; }
    public ICommand ShowReferencesCommand { get; }

    public ICommand NavigateNextCommand { get; }
    public ICommand NavigatePreviousCommand { get; }

    public RowWizardViewModel(
        Collection collection,
        IItemService itemService,
        ITemplateService templateService,
        ICollectionService collectionService,
        Item? existingItem = null)
    {
        _collection = collection;
        _itemService = itemService;
        _templateService = templateService;
        _collectionService = collectionService;
        _existingItem = existingItem;

        SubmitRowCommand = new AsyncRelayCommand(SubmitRowAsync, CanSubmit);
        UploadImageCommand = new RelayCommand<FieldInputViewModel>(UploadImage);
        OpenReferencePickerCommand = new RelayCommand<FieldInputViewModel>(OpenReferencePicker);
        RemoveReferenceCommand = new RelayCommand<ReferenceSelectionViewModel>(RemoveReference);
        ShowReferencesCommand = new RelayCommand<FieldInputViewModel>(ShowReferences);

        NavigateNextCommand = new RelayCommand(NavigateNext, CanNavigateNext);
        NavigatePreviousCommand = new RelayCommand(NavigatePrevious, CanNavigatePrevious);

        InitializeAsync();
    }

    private bool CanNavigateNext() => _existingItem?.NextItemId is not null;
    private bool CanNavigatePrevious() => _existingItem?.PreviousItemId is not null;

    // Triggers a navigation request to the next item in the sequence and closes the current wizard instance.
    private void NavigateNext()
    {
        if (_existingItem?.NextItemId is null)
            return;

        RequestNavigateToItemId?.Invoke(_existingItem.NextItemId.Value);
        CloseAction?.Invoke();
    }

    // Triggers a navigation request to the previous item in the sequence and closes the current wizard instance.
    private void NavigatePrevious()
    {
        if (_existingItem?.PreviousItemId is null)
            return;

        RequestNavigateToItemId?.Invoke(_existingItem.PreviousItemId.Value);
        CloseAction?.Invoke();
    }

    private bool CanSubmit() => Fields.All(IsFieldValueProvided);

    // Determines if a field contains a valid entry based on its specific data type requirements.
    private bool IsFieldValueProvided(FieldInputViewModel field)
    {
        if (field.FieldType == FieldType.ItemReference)
            return true;

        if (field.Value == null)
            return false;

        if (field.Value is string str)
            return !string.IsNullOrWhiteSpace(str);

        if (field.Value is byte[] bytes)
            return bytes.Length > 0;

        return true;
    }

    // Preloads template definitions and existing item data to populate the wizard's input fields.
    private async void InitializeAsync()
    {
        try
        {
            var template = await _templateService.GetTemplateAsync(_collection.TemplateId, includeFields: true);
            if (template == null) return;

            var collections = await _collectionService.GetCollectionsAsync();

            ItemsByCollectionMap.Clear();
            _itemsById.Clear();

            foreach (var col in collections)
            {
                var items = (await _itemService.GetItemsForCollectionAsync(col.Id)).ToList();

                ItemsByCollectionMap[col.Name] = items;
                foreach (var item in items)
                    _itemsById[item.Id] = item;
            }

            Item? existingWithValues = _existingItem == null
                ? null
                : await _itemService.GetItemAsync(_existingItem.Id, includeFieldValues: true);

            Fields.Clear();
            foreach (var definition in template.Fields)
            {
                var input = new FieldInputViewModel
                {
                    FieldId = definition.Id,
                    FieldName = definition.Name,
                    FieldType = definition.FieldType,
                    AllowsMultipleReferences = definition.IsList
                };

                if (existingWithValues != null)
                {
                    var existingValue = existingWithValues.FieldValues.FirstOrDefault(v => v.FieldDefinitionId == definition.Id);
                    if (existingValue != null)
                        PopulateExistingValue(input, definition, existingValue);
                }

                input.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(FieldInputViewModel.Value))
                        (SubmitRowCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                };

                Fields.Add(input);
            }

            (NavigateNextCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (NavigatePreviousCommand as RelayCommand)?.RaiseCanExecuteChanged();

            if (existingWithValues != null)
                InitializePrevNextSelectionsFromExistingItem();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error initializing form: {ex.Message}";
            StatusType = StatusMessageType.Error;
        }
    }

    // Synchronizes the previous and next item selection states based on the existing item's link properties.
    private void InitializePrevNextSelectionsFromExistingItem()
    {
        if (_existingItem == null)
            return;

        foreach (var field in Fields.Where(f => f.IsReferenceList))
        {
            var selectedIds = field.SelectedReferences.Select(r => r.ItemId).ToHashSet();

            if (_existingItem.PreviousItemId is int prev && selectedIds.Contains(prev))
                field.SelectedPreviousItemId = prev;

            if (_existingItem.NextItemId is int next && selectedIds.Contains(next))
                field.SelectedNextItemId = next;
        }
    }

    // Maps a database-stored field value to the appropriate UI input property based on its type.
    private void PopulateExistingValue(FieldInputViewModel input, FieldDefinition definition, FieldValue value)
    {
        switch (definition.FieldType)
        {
            case FieldType.Integer:
                input.Value = value.IntValue;
                break;
            case FieldType.Decimal:
                input.Value = value.DecimalValue;
                break;
            case FieldType.Date:
                input.Value = value.DateValue;
                break;
            case FieldType.Image:
                input.Value = value.ImageValue;
                break;
            case FieldType.ItemReference:
                if (definition.IsList)
                {
                    var ids = value.References?.Select(r => r.RelatedItemId).ToList() ?? new List<int>();
                    ApplySelections(input, ids);
                }
                else
                {
                    input.Value = value.RelatedItemId;
                    if (value.RelatedItemId.HasValue)
                    {
                        var item = GetItemById(value.RelatedItemId.Value);
                        if (item != null)
                            input.DisplayValue = BuildItemLabel(item);
                    }
                }
                break;
            default:
                input.Value = value.TextValue;
                break;
        }
    }

    // Displays a dialog for selecting one or more items to be referenced by the current field.
    private void OpenReferencePicker(FieldInputViewModel? field)
    {
        if (field == null || !field.IsReference) return;

        var preselected = field.IsReferenceList
            ? field.SelectedReferences.Select(r => r.ItemId).ToArray()
            : field.Value is int current ? new[] { current } : Array.Empty<int>();

        var picker = new ReferencePickerWindow(ItemsByCollectionMap, field.IsReferenceList, preselected.Any() ? preselected : null)
        {
            Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
        };

        if (picker.ShowDialog() != true)
            return;

        if (field.IsReferenceList)
        {
            ApplySelections(field, picker.SelectedItemIds ?? Array.Empty<int>());
        }
        else if (picker.SelectedItemId.HasValue)
        {
            var item = GetItemById(picker.SelectedItemId.Value);
            UpdateSingleReference(field, item);
        }
    }

    // Updates a multi-reference field with a new set of selected item references.
    private void ApplySelections(FieldInputViewModel field, IReadOnlyCollection<int> ids)
    {
        if (!field.IsReferenceList)
            return;

        if (ids == null || ids.Count == 0)
        {
            field.ClearSelectedReferences();
            field.SelectedPreviousItemId = null;
            field.SelectedNextItemId = null;
            return;
        }

        var references = ids
            .Select(GetItemById)
            .Where(item => item != null)
            .Select(item => new ReferenceSelectionViewModel(field, item!.Id, BuildItemLabel(item!)))
            .ToList();

        field.ReplaceSelectedReferences(references);

        var set = field.SelectedReferences.Select(r => r.ItemId).ToHashSet();
        if (field.SelectedPreviousItemId is int prev && !set.Contains(prev))
            field.SelectedPreviousItemId = null;
        if (field.SelectedNextItemId is int next && !set.Contains(next))
            field.SelectedNextItemId = null;
    }

    // Updates a single-reference field with a specific item and updates its display label.
    private void UpdateSingleReference(FieldInputViewModel field, Item? item)
    {
        if (item == null)
        {
            field.Value = null;
            field.DisplayValue = string.Empty;
        }
        else
        {
            field.Value = item.Id;
            field.DisplayValue = BuildItemLabel(item);
        }
    }

    // Opens a file dialog to select an image and stores its raw bytes in the field value.
    private void UploadImage(FieldInputViewModel? field)
    {
        if (field == null || !field.IsImage) return;

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Item Image",
            Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                field.Value = System.IO.File.ReadAllBytes(dialog.FileName);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to load image: {ex.Message}";
                StatusType = StatusMessageType.Error;
            }
        }
    }

    // Processes all field inputs and either creates a new item or updates an existing one in the database.
    private async Task SubmitRowAsync()
    {
        try
        {
            StatusMessage = string.Empty;

            if (!Fields.All(IsFieldValueProvided))
            {
                StatusMessage = "Please fill in all required fields to save the item.";
                StatusType = StatusMessageType.Error;
                return;
            }

            var inputs = new List<NewItemFieldValueInput>();

            foreach (var field in Fields)
            {
                if (field.FieldType == FieldType.ItemReference && field.IsReferenceList)
                {
                    var ids = field.SelectedReferences.Select(r => r.ItemId).Distinct().ToList();
                    if (ids.Count == 0)
                        continue;

                    inputs.Add(new NewItemFieldValueInput
                    {
                        FieldDefinitionId = field.FieldId,
                        RelatedItemIds = ids
                    });

                    continue;
                }

                if (field.Value == null)
                    continue;

                if (field.Value is string s && string.IsNullOrWhiteSpace(s))
                    continue;

                var input = new NewItemFieldValueInput { FieldDefinitionId = field.FieldId };

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

            if (_existingItem != null)
            {
                await _itemService.UpdateItemAsync(
                    _existingItem.Id,
                    inputs,
                    _existingItem.PreviousItemId,
                    _existingItem.NextItemId);

                DialogResult = true;
            }
            else
            {
                await _itemService.CreateItemAsync(_collection.Id, inputs, null, null);
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

    // Removes a specific item reference from a field's selection and updates the associated UI chips.
    private void RemoveReference(ReferenceSelectionViewModel? selection)
    {
        if (selection?.Owner == null) return;

        var owner = selection.Owner;
        owner.SelectedReferences.Remove(selection);

        var set = owner.SelectedReferences.Select(r => r.ItemId).ToHashSet();
        if (owner.SelectedPreviousItemId is int prev && !set.Contains(prev))
            owner.SelectedPreviousItemId = null;
        if (owner.SelectedNextItemId is int next && !set.Contains(next))
            owner.SelectedNextItemId = null;
    }

    // Opens a preview window to display detailed information about the currently selected item references.
    private void ShowReferences(FieldInputViewModel? field)
    {
        if (field == null || !field.IsReference)
            return;

        if (!field.IsReferenceList)
        {
            if (field.Value is not int id)
                return;

            var found = GetItemById(id);
            if (found == null)
                return;

            var preview = new ReferencePreviewWindow(new[] { found })
            {
                Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
            };
            preview.ShowDialog();
            return;
        }

        var items = field.SelectedReferences
            .Select(r => GetItemById(r.ItemId))
            .Where(i => i != null)
            .ToList();

        if (items.Count == 0)
            return;

        var listPreview = new ReferencePreviewWindow(items!, allowRemove: true)
        {
            Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
        };
        listPreview.ShowDialog();

        var removed = listPreview.RemovedItemIds;
        if (removed != null && removed.Count > 0)
        {
            var toRemove = field.SelectedReferences.Where(r => removed.Contains(r.ItemId)).ToList();
            foreach (var r in toRemove)
                field.SelectedReferences.Remove(r);

            var set = field.SelectedReferences.Select(r => r.ItemId).ToHashSet();
            if (field.SelectedPreviousItemId is int prev && !set.Contains(prev))
                field.SelectedPreviousItemId = null;
            if (field.SelectedNextItemId is int next && !set.Contains(next))
                field.SelectedNextItemId = null;
        }
    }

    private Item? GetItemById(int id) => _itemsById.TryGetValue(id, out var item) ? item : null;

    // Generates a display-friendly label for an item by using its first available text field or its unique ID.
    private static string BuildItemLabel(Item item)
        => item.FieldValues.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v.TextValue))?.TextValue
           ?? $"Item #{item.Id}";

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

// Represents the data and UI state for a single field within the item wizard.
public class FieldInputViewModel : INotifyPropertyChanged
{
    private FieldType _fieldType;
    private object? _value;
    private string _displayValue = string.Empty;
    private bool _allowsMultipleReferences;

    public FieldInputViewModel()
    {
        SelectedReferences.CollectionChanged += SelectedReferencesChanged;
    }

    public int FieldId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string Name => FieldName;

    public FieldType FieldType
    {
        get => _fieldType;
        set
        {
            if (_fieldType == value) return;
            _fieldType = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsText));
            OnPropertyChanged(nameof(IsDate));
            OnPropertyChanged(nameof(IsImage));
            OnPropertyChanged(nameof(IsReference));
            OnPropertyChanged(nameof(IsReferenceList));
            OnPropertyChanged(nameof(IsReferenceSingle));
            OnPropertyChanged(nameof(CanShowReferences));
        }
    }

    public bool AllowsMultipleReferences
    {
        get => _allowsMultipleReferences;
        set
        {
            if (_allowsMultipleReferences == value) return;
            _allowsMultipleReferences = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsReferenceList));
            OnPropertyChanged(nameof(IsReferenceSingle));
            OnPropertyChanged(nameof(CanShowReferences));
        }
    }

    public object? Value
    {
        get => _value;
        set
        {
            if (_value == value) return;
            _value = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanShowReferences));
        }
    }

    public string DisplayValue
    {
        get => _displayValue;
        set
        {
            if (_displayValue == value) return;
            _displayValue = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<ReferenceSelectionViewModel> SelectedReferences { get; } = new();

    public bool HasSelectedReferences => SelectedReferences.Any();

    public bool IsText => FieldType == FieldType.Text || FieldType == FieldType.Integer || FieldType == FieldType.Decimal;
    public bool IsDate => FieldType == FieldType.Date;
    public bool IsImage => FieldType == FieldType.Image;
    public bool IsReference => FieldType == FieldType.ItemReference;
    public bool IsReferenceList => IsReference && AllowsMultipleReferences;
    public bool IsReferenceSingle => IsReference && !AllowsMultipleReferences;
    public bool CanShowReferences =>
        (IsReferenceSingle && Value is int) ||
        (IsReferenceList && HasSelectedReferences);

    private int? _selectedPreviousItemId;
    public int? SelectedPreviousItemId
    {
        get => _selectedPreviousItemId;
        set
        {
            if (_selectedPreviousItemId == value) return;
            _selectedPreviousItemId = value;
            OnPropertyChanged();
        }
    }

    private int? _selectedNextItemId;
    public int? SelectedNextItemId
    {
        get => _selectedNextItemId;
        set
        {
            if (_selectedNextItemId == value) return;
            _selectedNextItemId = value;
            OnPropertyChanged();
        }
    }

    // Completely updates the collection of selected item references with a new set of values.
    public void ReplaceSelectedReferences(IEnumerable<ReferenceSelectionViewModel> references)
    {
        SelectedReferences.CollectionChanged -= SelectedReferencesChanged;
        SelectedReferences.Clear();

        foreach (var reference in references)
            SelectedReferences.Add(reference);

        SelectedReferences.CollectionChanged += SelectedReferencesChanged;
        SelectedReferencesChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public void ClearSelectedReferences()
    {
        SelectedReferences.Clear();
    }

    private void SelectedReferencesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasSelectedReferences));
        OnPropertyChanged(nameof(CanShowReferences));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

// Acts as a lightweight data container for an item reference displayed as a selectable chip in the UI.
public class ReferenceSelectionViewModel
{
    public ReferenceSelectionViewModel(FieldInputViewModel owner, int itemId, string displayValue)
    {
        Owner = owner;
        ItemId = itemId;
        DisplayValue = displayValue;
    }

    public FieldInputViewModel Owner { get; }
    public int ItemId { get; }
    public string DisplayValue { get; }
}

internal static class EnumerableExtensions
{
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : class
        => source.Where(item => item != null)!;

    public static IEnumerable<T> Yield<T>(this T? item) where T : class
    {
        if (item != null)
            yield return item;
    }
}