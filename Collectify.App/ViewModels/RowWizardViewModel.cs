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

public class RowWizardViewModel : INotifyPropertyChanged
{
    private readonly Collection _collection;
    private readonly IItemService _itemService;
    private readonly ITemplateService _templateService;
    private readonly ICollectionService _collectionService;
    private readonly Item? _existingItem;

    public Action? CloseAction { get; set; }
    
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

            // Load Lookups for References
            var collections = await _collectionService.GetCollectionsAsync();
            ItemsByCollectionMap.Clear();
            foreach (var col in collections)
            {
                var items = await _itemService.GetItemsForCollectionAsync(col.Id);
                ItemsByCollectionMap[col.Name] = items.ToList();
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

            if (_existingItem != null)
            {
                 await _itemService.UpdateItemAsync(_existingItem.Id, inputs, _existingItem.PreviousItemId, _existingItem.NextItemId);
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

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

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