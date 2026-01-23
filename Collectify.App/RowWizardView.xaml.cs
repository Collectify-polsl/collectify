using Collectify.App.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Collectify.App;

// Manages the window for the multi-field item editor, linking the view model's closure logic to the window's dialog result.
public partial class RowWizardView : Window
{
    // Initializes the wizard window and sets up the callback to properly close the dialog when the view model completes its task.
    public RowWizardView(RowWizardViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.CloseAction = () =>
        {
            if (vm.DialogResult == true)
                this.DialogResult = true;
            Close();
        };
    }

    // Customizes the dynamic generation of columns by swapping standard fields for specialized templates when images or references are detected.
    private void DataGrid_OnAutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        string header = e.Column.Header.ToString() ?? "";

        if (header.Contains("(Image)"))
        {
            e.Column = new DataGridTemplateColumn
            {
                Header = e.Column.Header,
                CellTemplate = (DataTemplate)Resources["ImageCellTemplate"]
            };
        }
        else if (header.Contains("(ItemReference)"))
        {
            e.Column = new DataGridTemplateColumn
            {
                Header = e.Column.Header,
                CellTemplate = (DataTemplate)Resources["ReferenceCellTemplate"]
            };
        }
    }
}