using Collectify.App.Commands;
using Collectify.Model.Collection;
using Collectify.Model.Entities;
using Collectify.Model.Enums;
using Collectify.Model.InputModels;
using Collectify.Model.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
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

    private List<FieldDefinition> _fieldDefinitions = new();
    private Dictionary<int, string> _fieldToColumnName = new();
    private DataTable _dataTable = new();

    public Action? CloseAction { get; set; }
    public Dictionary<string, List<Item>> ItemsByCollectionMap { get; } = new();

    private DataView _newRowPreview;
    public DataView NewRowPreview
    {
        get => _newRowPreview;
        set { _newRowPreview = value; OnPropertyChanged(); }
    }

    public ICommand SubmitRowCommand { get; }
    public ICommand UploadImageCommand { get; }
    public ICommand OpenReferencePickerCommand { get; }

    public RowWizardViewModel(Collection collection, IItemService itemService,
                              ITemplateService templateService, ICollectionService collectionService)
    {
        _collection = collection;
        _itemService = itemService;
        _templateService = templateService;
        _collectionService = collectionService;

        SubmitRowCommand = new AsyncRelayCommand(SubmitRowAsync);
        UploadImageCommand = new RelayCommand<string>(UploadImage);
        OpenReferencePickerCommand = new RelayCommand<string>(OpenReferencePicker);

        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        var template = await _templateService.GetTemplateAsync(_collection.TemplateId, includeFields: true);
        if (template == null) return;

        _fieldDefinitions = template.Fields.ToList();

        // Load map for Reference Picker
        var collections = await _collectionService.GetCollectionsAsync();
        ItemsByCollectionMap.Clear();
        foreach (var col in collections)
        {
            var items = await _itemService.GetItemsForCollectionAsync(col.Id);
            ItemsByCollectionMap[col.Name] = items.ToList();
        }

        _dataTable = new DataTable();
        foreach (var field in _fieldDefinitions)
        {
            string header = $"{field.Name} ({field.FieldType})";
            Type colType = field.FieldType switch
            {
                FieldType.Integer => typeof(int),
                FieldType.Decimal => typeof(decimal),
                FieldType.Date => typeof(DateOnly),
                FieldType.Image => typeof(byte[]),
                FieldType.ItemReference => typeof(int),
                _ => typeof(string)
            };
            _dataTable.Columns.Add(new DataColumn(header, colType) { AllowDBNull = true });
            _fieldToColumnName[field.Id] = header;
        }

        _dataTable.Rows.Add(_dataTable.NewRow());
        NewRowPreview = _dataTable.DefaultView;
    }

    private void OpenReferencePicker(string? columnName)
    {
        if (string.IsNullOrEmpty(columnName)) return;
       
        var picker = new ReferencePickerWindow(ItemsByCollectionMap);
        picker.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);

        if (picker.ShowDialog() == true)
        {
            var row = _dataTable.Rows[0];
            row.BeginEdit();
            row[columnName] = picker.SelectedItemId;
            row.EndEdit();

            OnPropertyChanged(nameof(NewRowPreview));
        }
    }

    private void UploadImage(string? columnName)
    {
        if (string.IsNullOrEmpty(columnName)) return;

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

                var row = _dataTable.Rows[0];
                row.BeginEdit();
                row[columnName] = imageBytes;
                row.EndEdit(); // KLUCZOWE: Zatwierdza zmiany w DataTable

                // Powiadomienie UI o zmianie danych w tabeli
                OnPropertyChanged(nameof(NewRowPreview));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load image: {ex.Message}");
            }
        }
    }

    private async Task SubmitRowAsync()
    {
        try
        {
            var row = _dataTable.Rows[0];
            var inputs = new List<NewItemFieldValueInput>();

            foreach (var field in _fieldDefinitions)
            {
                string colName = _fieldToColumnName[field.Id];
                object cellValue = row[colName];

                if (cellValue == DBNull.Value || cellValue == null) continue;

                var input = new NewItemFieldValueInput { FieldDefinitionId = field.Id };
                switch (field.FieldType)
                {
                    case FieldType.Integer: input.IntValue = Convert.ToInt32(cellValue); break;
                    case FieldType.Decimal: input.DecimalValue = Convert.ToDecimal(cellValue); break;
                    case FieldType.Date: input.DateValue = ((DateOnly)cellValue).ToDateTime(TimeOnly.MinValue); break;
                    case FieldType.Image: input.ImageValue = (byte[])cellValue; break;
                    case FieldType.ItemReference: input.RelatedItemId = Convert.ToInt32(cellValue); break;
                    default: input.TextValue = cellValue.ToString(); break;
                }
                inputs.Add(input);
            }

            if (!inputs.Any()) return;
            await _itemService.CreateItemAsync(_collection.Id, inputs, null, null);
            MessageBox.Show("Item added successfully!", "Success");
            CloseAction?.Invoke();
        }
        catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}"); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}