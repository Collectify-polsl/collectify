using Collectify.App.Commands;
using Collectify.Model.Enums;
using Collectify.Model.InputModels;
using Collectify.Model.Interfaces;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Linq;
using System.Threading.Tasks;

namespace Collectify.App.ViewModels;

public class NewTemplateViewModel : INotifyPropertyChanged
{
    private readonly ITemplateService _templateService;

    public Action? CloseAction { get; set; }

    // ====== NAZWA SZABLONU ======
    private string _templateName = string.Empty;
    public string TemplateName
    {
        get => _templateName;
        set
        {
            _templateName = value;
            OnPropertyChanged();
            (SaveTemplateCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    // ====== DODAWANIE POLA ======
    private string _newColumnName = string.Empty;
    public string NewColumnName
    {
        get => _newColumnName;
        set
        {
            _newColumnName = value;
            OnPropertyChanged();
            (AddColumnCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    public ObservableCollection<FieldType> DataTypeList { get; }
        = new() { FieldType.Text, FieldType.Integer, FieldType.Date };

    private FieldType _selectedFieldType = FieldType.Text;
    public FieldType SelectedFieldType
    {
        get => _selectedFieldType;
        set { _selectedFieldType = value; OnPropertyChanged(); }
    }

    // ====== LISTA PÓL ======
    public ObservableCollection<ColumnItem> Columns { get; } = new();

    // ====== KOMENDY ======
    public ICommand AddColumnCommand { get; }
    public ICommand RemoveColumnCommand { get; }
    public ICommand SaveTemplateCommand { get; }

    public NewTemplateViewModel(ITemplateService templateService)
    {
        _templateService = templateService;

        AddColumnCommand = new RelayCommand(AddColumn, CanAddColumn);
        RemoveColumnCommand = new RelayCommand<ColumnItem>(RemoveColumn);
        SaveTemplateCommand = new AsyncRelayCommand(SaveTemplateAsync, CanSave);

        Columns.CollectionChanged += (_, __) =>
            (SaveTemplateCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
    }

    // ====== LOGIKA ======
    private bool CanAddColumn() =>
        !string.IsNullOrWhiteSpace(NewColumnName) &&
        !Columns.Any(c =>
            c.Name.Equals(NewColumnName.Trim(), StringComparison.OrdinalIgnoreCase));

    private void AddColumn()
    {
        Columns.Add(new ColumnItem
        {
            Name = NewColumnName.Trim(),
            DataType = SelectedFieldType
        });
        NewColumnName = string.Empty;
    }

    private void RemoveColumn(ColumnItem column)
    {
        if (Columns.Contains(column))
            Columns.Remove(column);
    }

    private bool CanSave() =>
        !string.IsNullOrWhiteSpace(TemplateName) &&
        Columns.Any();

    private async Task SaveTemplateAsync()
    {
        try
        {
            var fields = Columns.Select(c =>
                new TemplateFieldDefinitionInput
                {
                    Name = c.Name,
                    FieldType = c.DataType,
                    IsList = false
                }).ToList();

            await _templateService.CreateTemplateAsync(TemplateName, fields);

            MessageBox.Show(
                "Template saved!",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            CloseAction?.Invoke();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Save error: {ex.Message}");
        }
    }

    // ====== INotifyPropertyChanged ======
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

