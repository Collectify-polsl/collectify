using Collectify.App.Commands;
using Collectify.Model.Enums;
using Collectify.Model.InputModels;
using Collectify.Model.Interfaces;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace Collectify.App.ViewModels;

public class NewCollectionViewModel : INotifyPropertyChanged
{
    private readonly ICollectionService _collectionService;
    private readonly ITemplateService _templateService;

    // Akcja zamykająca okno (ustawiana w Code Behind)
    public Action? CloseAction { get; set; }

    // --- DANE FORMULARZA ---
    private string _collectionName = string.Empty;
    public string CollectionName
    {
        get => _collectionName;
        set { _collectionName = value; OnPropertyChanged();
            (SaveCollectionCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();

        }
    }

    private string _collectionDescription = string.Empty;
    public string CollectionDescription
    {
        get => _collectionDescription;
        set { _collectionDescription = value; OnPropertyChanged(); }
    }

    // --- DANE KOLUMN ---
    private string _newColumnName = string.Empty;
    public string NewColumnName
    {
        get => _newColumnName;
        set { _newColumnName = value; OnPropertyChanged();

            (AddColumnCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    // Ograniczenie do String i Int
    public IEnumerable<FieldType> DataTypeList { get; } = new[]
    {
        FieldType.Text,
        FieldType.Integer
    };

    private FieldType _selectedNewColumnType = FieldType.Text;
    public FieldType SelectedNewColumnType
    {
        get => _selectedNewColumnType;
        set
        {
            if (_selectedNewColumnType != value)
            {
                _selectedNewColumnType = value;
                OnPropertyChanged();

                (SaveCollectionCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public ObservableCollection<ColumnItem> AddedColumns { get; } = new();

    // --- KOMENDY ---
    public ICommand AddColumnCommand { get; }
    public ICommand RemoveColumnCommand { get; }
    public ICommand SaveCollectionCommand { get; }

    // --- KONSTRUKTOR ---
    public NewCollectionViewModel(ICollectionService collectionService, ITemplateService templateService)
    {
        _collectionService = collectionService;
        _templateService = templateService;

        AddColumnCommand = new RelayCommand(AddColumn, CanAddColumn);
        // Używamy wersji generycznej RelayCommand<T>
        RemoveColumnCommand = new RelayCommand<ColumnItem>(RemoveColumn);
        SaveCollectionCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        AddedColumns.CollectionChanged += (s, e) =>
        {
            (SaveCollectionCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        };
    }

    // --- LOGIKA ---
    private bool CanAddColumn() => !string.IsNullOrWhiteSpace(NewColumnName);

    private void AddColumn()
    {
        AddedColumns.Add(new ColumnItem
        {
            Name = NewColumnName,
            DataType = SelectedNewColumnType
        });
        NewColumnName = string.Empty;
    }

    private void RemoveColumn(ColumnItem item)
    {
        if (item != null && AddedColumns.Contains(item))
            AddedColumns.Remove(item);
    }

    private bool CanSave() => !string.IsNullOrWhiteSpace(CollectionName) && AddedColumns.Any();

    private async Task SaveAsync()
    {
        try
        {
            // 1. Utwórz Szablon
            var fieldInputs = AddedColumns.Select(c => new TemplateFieldDefinitionInput
            {
                Name = c.Name,
                FieldType = c.DataType,
                IsList = false
            }).ToList();

            var template = await _templateService.CreateTemplateAsync($"Szablon: {CollectionName}", fieldInputs);

            // 2. Utwórz Kolekcję
            await _collectionService.CreateCollectionAsync(template.Id, CollectionName, CollectionDescription);

            MessageBox.Show("Kolekcja została utworzona!", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);

            // 3. Zamknij okno
            CloseAction?.Invoke();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Błąd: {ex.Message}");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void AddColumnCommand_RaiseCanExecuteChanged() =>
           (AddColumnCommand as AsyncRelayCommand).RaiseCanExecuteChanged();

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
public class ColumnItem
{
    public string Name { get; set; } = string.Empty;
    public FieldType DataType { get; set; }
}