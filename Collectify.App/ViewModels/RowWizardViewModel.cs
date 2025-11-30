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

    // Przechowujemy definicje pól (Schema)
    private List<FieldDefinition> _fieldDefinitions = new();

    // Mapa: Nazwa kolumny (z typem w nawiasie) -> FieldDefinitionId
    private Dictionary<int, string> _fieldToColumnName = new();
    private DataTable _dataTable;

    public Action? CloseAction { get; set; }

    // --- BINDINGI ---
    private DataView _newRowPreview;
    public DataView NewRowPreview
    {
        get => _newRowPreview;
        set { _newRowPreview = value; OnPropertyChanged(); }
    }

    public ICommand SubmitRowCommand { get; }

    // --- KONSTRUKTOR ---
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
            // Tworzymy nazwę kolumny z typem (np. "Rok (Integer)")
            string headerWithInfo = $"{field.Name} ({field.FieldType})";

            // NOWOŚĆ: Określamy prawdziwy typ danych dla kolumny
            Type colType = field.FieldType switch
            {
                FieldType.Integer => typeof(int),
                FieldType.Decimal => typeof(decimal),
                FieldType.Date => typeof(DateTime),
                _ => typeof(string)
            };

            _dataTable.Columns.Add(headerWithInfo, colType);
            _fieldToColumnName[field.Id] = headerWithInfo;
        }

        var row = _dataTable.NewRow();
        _dataTable.Rows.Add(row);

        NewRowPreview = _dataTable.DefaultView;
    }

    // --- LOGIKA SUBMIT ---
    private async Task SubmitRowAsync()
    {
        try
        {
            var row = _dataTable.Rows[0];
            var inputs = new List<NewItemFieldValueInput>();

            foreach (var field in _fieldDefinitions)
            {
                string colName = _fieldToColumnName[field.Id];

                // Pobieramy obiekt z komórki
                object cellValue = row[colName];

                // Pomijamy puste komórki (DBNull)
                if (cellValue == DBNull.Value || cellValue == null || string.IsNullOrWhiteSpace(cellValue.ToString()))
                    continue;

                var input = new NewItemFieldValueInput
                {
                    FieldDefinitionId = field.Id
                };

                // Typy są poprawne; wystarczy rzutowanie (casting)
                switch (field.FieldType)
                {
                    case FieldType.Integer:
                        input.IntValue = (int)cellValue;
                        break;
                    case FieldType.Decimal:
                        input.DecimalValue = (decimal)cellValue;
                        break;
                    case FieldType.Date:
                        input.DateValue = (DateTime)cellValue;
                        break;
                    default:
                        input.TextValue = cellValue.ToString();
                        break;
                }
                inputs.Add(input);
            }

            if (inputs.Count == 0)
            {
                MessageBox.Show("Niewłaściwy input!", "Uwaga");
                return;
            }

            await _itemService.CreateItemAsync(_collection.Id, inputs, null, null);

            MessageBox.Show("Dodano!", "Sukces");
            CloseAction?.Invoke();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Błąd zapisu: {ex.Message}");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}