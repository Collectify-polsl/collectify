using Collectify.App.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Collectify.App;

public partial class RowWizardView : Window
{
    public RowWizardView(RowWizardViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.CloseAction = Close;
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