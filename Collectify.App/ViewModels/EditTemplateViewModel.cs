using Collectify.App.Commands;
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

public class EditTemplateViewModel : INotifyPropertyChanged
{
    private readonly ITemplateService _templateService;

    public Action? CloseAction { get; set; }

    public ObservableCollection<Template> TemplateList { get; } = new();
    private Template? _selectedTemplate;
    private string _templateName = string.Empty;

    public Template? SelectedTemplate
    {
        get => _selectedTemplate;
        set
        {
            _selectedTemplate = value;
            OnPropertyChanged();
            LoadTemplateFields();
            RefreshSaveState();
            (DeleteTemplateCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    public string TemplateName
    {
        get => _templateName;
        set
        {
            _templateName = value;
            OnPropertyChanged();
            RefreshSaveState(); // Kluczowe dla aktywacji przycisku przy zmianie tekstu
        }
    }

    public ObservableCollection<ColumnItem> Columns { get; } = new();

    public ObservableCollection<FieldType> DataTypeList { get; } = new()
    {
        FieldType.Text, FieldType.Integer, FieldType.Decimal,
        FieldType.Date, FieldType.Image, FieldType.ItemReference
    };

    private string _newColumnName = string.Empty;
    public string NewColumnName { get => _newColumnName; set { _newColumnName = value; OnPropertyChanged(); } }

    private FieldType _selectedFieldType = FieldType.Text;
    public FieldType SelectedFieldType { get => _selectedFieldType; set { _selectedFieldType = value; OnPropertyChanged(); } }

    private readonly ObservableCollection<ColumnItem> _removedColumns = new();

    public ICommand AddColumnCommand { get; }
    public ICommand RemoveColumnCommand { get; }
    public ICommand SaveTemplateCommand { get; }
    public ICommand DeleteTemplateCommand { get; }

    // Właściwość do bindowania IsEnabled w XAML (opcjonalnie)
    public bool CanSubmit => CanSave();

    public EditTemplateViewModel(ITemplateService templateService)
    {
        _templateService = templateService;

        AddColumnCommand = new RelayCommand(AddColumn);
        RemoveColumnCommand = new RelayCommand<ColumnItem>(RemoveColumn);

        // Komenda korzysta z właściwości CanSubmit
        SaveTemplateCommand = new AsyncRelayCommand(SaveAsync, () => CanSubmit);
        DeleteTemplateCommand = new AsyncRelayCommand(DeleteAsync, CanDelete);

        Columns.CollectionChanged += OnColumnsChanged;

        LoadTemplatesAsync();
    }

    private void RefreshSaveState()
    {
        OnPropertyChanged(nameof(CanSubmit));
        (SaveTemplateCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
    }

    private void OnColumnsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshSaveState();
    }

    private bool IsDirty()
    {
        if (SelectedTemplate == null) return false;

        bool nameChanged = TemplateName != SelectedTemplate.Name;
        bool columnsRemoved = _removedColumns.Any();
        bool columnsAdded = Columns.Any(c => !c.Id.HasValue);

        return nameChanged || columnsRemoved || columnsAdded;
    }

    private bool CanSave()
    {
        return SelectedTemplate != null &&
               !string.IsNullOrWhiteSpace(TemplateName) &&
               Columns.Any() &&
               IsDirty();
    }

    private async void LoadTemplatesAsync()
    {
        var templates = await _templateService.GetAllTemplatesAsync();
        TemplateList.Clear();
        foreach (var t in templates)
            TemplateList.Add(t);

        if (TemplateList.Any())
            SelectedTemplate = TemplateList.First();
    }

    private async void LoadTemplateFields()
    {
        Columns.Clear();
        _removedColumns.Clear();
        if (SelectedTemplate == null) return;

        var fullTemplate = await _templateService.GetTemplateAsync(SelectedTemplate.Id, includeFields: true);
        if (fullTemplate == null) return;

        _templateName = fullTemplate.Name; // Ustawiamy pole prywatne, żeby nie wywołać RefreshSaveState za wcześnie
        OnPropertyChanged(nameof(TemplateName));

        foreach (var f in fullTemplate.Fields)
        {
            Columns.Add(new ColumnItem
            {
                Id = f.Id,
                Name = f.Name,
                DataType = f.FieldType
            });
        }

        RefreshSaveState(); // Odśwież stan po załadowaniu (powinien być false)
    }

    private void AddColumn()
    {
        if (string.IsNullOrWhiteSpace(NewColumnName)) return;
        if (Columns.Any(c => c.Name.Equals(NewColumnName, StringComparison.OrdinalIgnoreCase))) return;

        Columns.Add(new ColumnItem
        {
            Name = NewColumnName.Trim(),
            DataType = SelectedFieldType
        });
        NewColumnName = string.Empty;
        // RefreshSaveState wywoła się automatycznie przez OnColumnsChanged
    }

    private void RemoveColumn(ColumnItem item)
    {
        if (!Columns.Contains(item)) return;

        if (item.Id.HasValue)
            _removedColumns.Add(item);

        Columns.Remove(item);
        // RefreshSaveState wywoła się automatycznie przez OnColumnsChanged
    }

    private async Task SaveAsync()
    {
        if (!CanSave()) return;

        try
        {
            if (TemplateName != SelectedTemplate!.Name)
                await _templateService.UpdateTemplateAsync(SelectedTemplate.Id, TemplateName);

            foreach (var removed in _removedColumns)
            {
                if (removed.Id.HasValue)
                    await _templateService.RemoveFieldAsync(removed.Id.Value);
            }
            _removedColumns.Clear();

            foreach (var col in Columns.Where(c => !c.Id.HasValue))
            {
                var newField = await _templateService.AddFieldAsync(
                    SelectedTemplate.Id, col.Name, col.DataType, false);
                col.Id = newField.Id;
            }

            MessageBox.Show("Template saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            CloseAction?.Invoke();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving template: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool CanDelete() => SelectedTemplate != null;

    private async Task DeleteAsync()
    {
        if (SelectedTemplate == null) return;

        var result = MessageBox.Show($"Delete template '{TemplateName}'?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            await _templateService.DeleteTemplateAsync(SelectedTemplate.Id);
            LoadTemplatesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}