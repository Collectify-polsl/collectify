using System.Windows;
using System.Windows.Controls;

namespace Collectify.App;

public partial class SingleCollectionView : UserControl
{
    public SingleCollectionView()
    {
        InitializeComponent();
    }

    private void DataGrid_OnAutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        if (e.PropertyName == "Id")
        {
            e.Cancel = true;
            return;
        }
        if (e.PropertyType == typeof(byte[]))
        {
            e.Column = new DataGridTemplateColumn
            {
                Header = e.Column.Header,
                CellTemplate = (DataTemplate)Resources["ImageCellTemplate"]
            };
        }
        else if (e.PropertyType == typeof(int))
        {
            e.Column = new DataGridTemplateColumn
            {
                Header = e.Column.Header,
                CellTemplate = (DataTemplate)Resources["ReferenceCellTemplate"]
            };
        }
    }
}