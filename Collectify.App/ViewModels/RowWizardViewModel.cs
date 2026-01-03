using Collectify.App.Commands;
using Collectify.Model.Collection;
using Collectify.Model.Entities;
using Collectify.Model.Enums;
using Collectify.Model.InputModels;
using Collectify.Model.Interfaces;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace Collectify.App.ViewModels;

public class RowWizardViewModel : INotifyPropertyChanged
{
    private readonly Collection _collection;
    private readonly IItemService _itemService;
    private readonly ITemplateService _templateService;

    private List<FieldDefinition> _fieldDefinitions = new();
    private Dictionary<int, string> _fieldToColumnName = new();
    private DataTable _dataTable;

    public Action? CloseAction { get; set; }

    private DataView _newRowPreview;
    public DataView NewRowPreview
    {
        get => _newRowPreview;
        set { _newRowPreview = value; OnPropertyChanged(); }
    }

    public ICommand SubmitRowCommand { get; }

    public RowWizardViewModel(Collection collection, IItemService itemService, ITemplateService templateService)
    {
        _collection = collection;
        _itemService = itemService;
        _templateService = templateService;

        SubmitRowCommand = new AsyncRelayCommand(SubmitRowAsync);

        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        var template = await _templateService.GetTemplateAsync(_collection.TemplateId, includeFields: true);
        if (template == null) return;

        _fieldDefinitions = template.Fields.ToList();
        _dataTable = new DataTable();
        _fieldToColumnName.Clear();

        foreach (var field in _fieldDefinitions)
        {
            string headerWithInfo = $"{field.Name} ({field.FieldType})";

            Type colType = field.FieldType switch
            {
                FieldType.Integer => typeof(int),
                FieldType.Decimal => typeof(decimal),
                FieldType.Date => typeof(DateOnly),
                _ => typeof(string)
            };

            _dataTable.Columns.Add(headerWithInfo, colType);
            _fieldToColumnName[field.Id] = headerWithInfo;
        }

        var row = _dataTable.NewRow();
        _dataTable.Rows.Add(row);

        NewRowPreview = _dataTable.DefaultView;
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

                if (cellValue == DBNull.Value || cellValue == null || string.IsNullOrWhiteSpace(cellValue.ToString()))
                    continue;

                var input = new NewItemFieldValueInput { FieldDefinitionId = field.Id };

                switch (field.FieldType)
                {
                    case FieldType.Integer:
                        input.IntValue = (int)cellValue;
                        break;
                    case FieldType.Decimal:
                        input.DecimalValue = (decimal)cellValue;
                        break;
                    case FieldType.Date:
                        var dateOnly = (DateOnly)cellValue;
                        input.DateValue = dateOnly.ToDateTime(TimeOnly.MinValue);
                        break;
                    default:
                        input.TextValue = cellValue.ToString();
                        break;
                }

                inputs.Add(input);
            }

            if (inputs.Count == 0)
            {
                MessageBox.Show("Invalid input!", "Warning");
                return;
            }

            await _itemService.CreateItemAsync(_collection.Id, inputs, null, null);

            MessageBox.Show("Item added successfully!", "Success");
            CloseAction?.Invoke();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Save error: {ex.Message}");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
