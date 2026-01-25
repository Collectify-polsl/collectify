using Collectify.App.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Collectify.App;

/// <summary>
/// Interaction logic for RowWizardView.xaml.
/// </summary>
public partial class RowWizardView : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RowWizardView"/> class.
    /// </summary>
    /// <param name="vm">The view model.</param>
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