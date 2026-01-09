using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

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

        // Pobieramy dostęp do struktury tabeli
        var grid = sender as DataGrid;
        var dataView = grid?.ItemsSource as DataView;
        var table = dataView?.Table;

        if (table != null && table.Columns.Contains(e.PropertyName))
        {
            var columnType = table.Columns[e.PropertyName].DataType;

            // 1. Sprawdzenie obrazka
            if (columnType == typeof(byte[]))
            {
                e.Column = new DataGridTemplateColumn
                {
                    Header = e.Column.Header,
                    CellTemplate = (DataTemplate)Resources["ImageCellTemplate"],
                    SortMemberPath = e.PropertyName
                };
                return;
            }

            // 2. PEWNIEJSZE sprawdzenie referencji
            // Szukamy w kolumnie czy jakikolwiek wiersz zawiera ReferenceValue
            // lub sprawdzamy czy typ to object (bo tak ustawiliśmy w ViewModelu dla referencji)
            bool isReference = false;
            foreach (DataRow row in table.Rows)
            {
                var value = row[e.PropertyName];
                if (value?.GetType().Name == "ReferenceValue")
                {
                    isReference = true;
                    break;
                }
            }

            if (isReference)
            {
                e.Column = new DataGridTemplateColumn
                {
                    Header = e.Column.Header,
                    CellTemplate = (DataTemplate)Resources["ReferenceCellTemplate"],
                    SortMemberPath = e.PropertyName
                };
                return;
            }
        }

        // 3. Reszta jako tekst (Liczby, Daty, Tekst)
        e.Column = new DataGridTextColumn
        {
            Header = e.Column.Header,
            Binding = new Binding(e.PropertyName),
            ElementStyle = new Style(typeof(TextBlock))
            {
                Setters = {
                new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center),
                new Setter(TextBlock.MarginProperty, new Thickness(10, 0, 10, 0))
            }
            }
        };
    }
    private void ItemsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is DataGrid grid && grid.SelectedItem != null)
        {
            // Przewija widok tak, aby wybrany element był widoczny
            grid.ScrollIntoView(grid.SelectedItem);
        }
    }
}