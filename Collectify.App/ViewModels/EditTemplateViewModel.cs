using Collectify.App.Commands;
using Collectify.Model.Entities;
using Collectify.Model.Enums;
using Collectify.Model.InputModels;
using Collectify.Model.Interfaces;
using System.Collections.ObjectModel;
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

    // List of all templates
    public ObservableCollection<Template> TemplateList { get; } = new();
    private Template? _selectedTemplate;
    public Template? SelectedTemplate
    {
        get => _selectedTemplate;
        set
        {
            _selectedTemplate = value;
            OnPropertyChanged();
            LoadTemplateFields();
        }
    }

    // Template name
    private string _templateName = string.Empty;
    public string TemplateName
    {
        get => _templateName;
        set { _templateName = value; OnPropertyChanged(); }
    }

    // Fields of the template
    public ObservableCollection<ColumnItem> Columns { get; } = new();

    // Add field
    public ObservableCollection<FieldType> DataTypeList { get; } = new() { FieldType.Text, FieldType.Integer, FieldType.Date };
    private string _newColumnName = string.Empty;
    public string NewColumnName { get => _newColumnName; set { _newColumnName = value; OnPropertyChanged(); } }
    private FieldType _selectedFieldType = FieldType.Text;
    public FieldType SelectedFieldType { get => _selectedFieldType; set { _selectedFieldType = value; OnPropertyChanged(); } }

    // Track removed columns to delete from DB
    private readonly ObservableCollection<ColumnItem> _removedColumns = new();

    // Commands
    public ICommand AddColumnCommand { get; }
    public ICommand RemoveColumnCommand { get; }
    public ICommand SaveTemplateCommand { get; }
    public ICommand DeleteTemplateCommand { get; }

    public EditTemplateViewModel(ITemplateService templateService)
    {
        _templateService = templateService;

        AddColumnCommand = new RelayCommand(AddColumn);
        RemoveColumnCommand = new RelayCommand<ColumnItem>(RemoveColumn);
        SaveTemplateCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        DeleteTemplateCommand = new AsyncRelayCommand(DeleteAsync, CanDelete);

        LoadTemplatesAsync();
    }

    // Load templates from DB
    private async void LoadTemplatesAsync()
    {
        var templates = await _templateService.GetAllTemplatesAsync();
        TemplateList.Clear();
        foreach (var t in templates)
            TemplateList.Add(t);

        if (TemplateList.Any())
            SelectedTemplate = TemplateList.First();
    }

    // Load template fields into Columns collection
    private async void LoadTemplateFields()
    {
        Columns.Clear();
        _removedColumns.Clear();
        if (SelectedTemplate == null) return;

        var fullTemplate = await _templateService.GetTemplateAsync(SelectedTemplate.Id, includeFields: true);
        if (fullTemplate == null) return;

        TemplateName = fullTemplate.Name;

        foreach (var f in fullTemplate.Fields)
        {
            Columns.Add(new ColumnItem
            {
                Id = f.Id, // Keep track of DB ID for deletion
                Name = f.Name,
                DataType = f.FieldType
            });
        }
    }

    // Add a new column (will be added to DB on save)
    private void AddColumn()
    {
        if (string.IsNullOrWhiteSpace(NewColumnName)) return;
        if (Columns.Any(c => c.Name.Equals(NewColumnName, System.StringComparison.OrdinalIgnoreCase))) return;

        Columns.Add(new ColumnItem
        {
            Name = NewColumnName.Trim(),
            DataType = SelectedFieldType
        });
        NewColumnName = string.Empty;
    }

    // Remove a column
    private void RemoveColumn(ColumnItem item)
    {
        if (!Columns.Contains(item)) return;

        Columns.Remove(item);

        // Only track for removal if it exists in DB (has Id)
        if (item.Id.HasValue)
            _removedColumns.Add(item);
    }

    private bool CanSave() => SelectedTemplate != null && !string.IsNullOrWhiteSpace(TemplateName);

    // Save template updates (name, new fields, removed fields)
    private async Task SaveAsync()
    {
        if (SelectedTemplate == null) return;

        try
        {
            // 1. Update template name
            if (TemplateName != SelectedTemplate.Name)
                await _templateService.UpdateTemplateAsync(SelectedTemplate.Id, TemplateName);

            // 2. Delete removed columns from DB
            foreach (var removed in _removedColumns)
            {
                if (removed.Id.HasValue)
                    await _templateService.RemoveFieldAsync(removed.Id.Value);
            }
            _removedColumns.Clear();

            // 3. Add new columns to DB (those without Id are new)
            foreach (var col in Columns.Where(c => !c.Id.HasValue))
            {
                var newField = await _templateService.AddFieldAsync(
                    SelectedTemplate.Id,
                    col.Name,
                    col.DataType,
                    isList: false);

                col.Id = newField.Id; // assign new DB id
            }

            MessageBox.Show("Template saved successfully.");
            LoadTemplateFields(); // reload to refresh IDs
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving template: {ex.Message}");
        }
    }

    private bool CanDelete() => SelectedTemplate != null;

    // Delete entire template
    private async Task DeleteAsync()
    {
        if (SelectedTemplate == null) return;

        var result = MessageBox.Show($"Delete template '{TemplateName}'? This cannot be undone.",
            "Confirm deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            await _templateService.DeleteTemplateAsync(SelectedTemplate.Id);
            MessageBox.Show("Template deleted.");
            LoadTemplatesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Cannot delete template: {ex.Message}");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

