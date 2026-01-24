using System;
using System.ComponentModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Collectify.App.ViewModels;

namespace Collectify.App;

// Handles the code-behind for the collection view, specifically managing dynamic DataGrid column generation and visual tree interactions.
public partial class SingleCollectionView : UserControl
{
    public SingleCollectionView()
    {
        InitializeComponent();
        this.DataContextChanged += SingleCollectionView_DataContextChanged;
        ItemsGrid.PreviewMouseDown += ItemsGrid_PreviewMouseDown;
    }

    // Deselects the current grid item if the user clicks on an empty area of the control.
    private void ItemsGrid_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var dependencyObject = (DependencyObject)e.OriginalSource;
        var row = FindParent<DataGridRow>(dependencyObject);

        if (row == null)
        {
            ItemsGrid.SelectedItem = null;
        }
    }

    // Recursively searches up the visual tree to find a parent of a specific type.
    public static T? FindParent<T>(DependencyObject? child) where T : DependencyObject
    {
        if (child == null) return null;
        DependencyObject parentObject = VisualTreeHelper.GetParent(child);
        if (parentObject == null) return null;
        if (parentObject is T parent) return parent;
        return FindParent<T>(parentObject);
    }

    // Manages subscription to ViewModel property changes whenever the view's data context is updated.
    private void SingleCollectionView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is SingleCollectionViewModel vm)
        {
            vm.PropertyChanged += ViewModel_PropertyChanged;
            if (vm.GridDataTable != null)
            {
                GenerateColumns(vm.GridDataTable);
            }
        }

        if (e.OldValue is SingleCollectionViewModel oldVm)
        {
            oldVm.PropertyChanged -= ViewModel_PropertyChanged;
        }
    }

    // Triggers a full column regeneration whenever the underlying DataTable in the ViewModel is swapped out.
    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SingleCollectionViewModel.GridDataTable))
        {
            if (sender is SingleCollectionViewModel vm && vm.GridDataTable != null)
            {
                ItemsGrid.SelectedItem = null;
                GenerateColumns(vm.GridDataTable);
            }
        }
    }

    // Dynamically builds DataGrid columns based on the DataTable schema, assigning templates for images and references.
    private void GenerateColumns(DataTable table)
    {
        ItemsGrid.Columns.Clear();

        foreach (DataColumn column in table.Columns)
        {
            string propertyName = column.ColumnName;

            if (propertyName == "Id" || propertyName.EndsWith("_CollectionName"))
                continue;

            DataGridColumn gridColumn;
            var columnType = column.DataType;

            if (columnType == typeof(byte[]))
            {
                gridColumn = new DataGridTemplateColumn
                {
                    Header = propertyName,
                    CellTemplate = (DataTemplate)Resources["ImageCellTemplate"],
                    SortMemberPath = propertyName
                };
            }
            else
            {
                var isReference = column.ExtendedProperties.ContainsKey("IsReference");

                if (isReference)
                {
                    gridColumn = new DataGridTemplateColumn
                    {
                        Header = propertyName,
                        CellTemplate = (DataTemplate)Resources["ReferenceCellTemplate"],
                        SortMemberPath = propertyName
                    };
                }
                else
                {
                    gridColumn = new DataGridTextColumn
                    {
                        Header = propertyName,
                        Binding = new Binding(propertyName),
                        ElementStyle = new Style(typeof(TextBlock))
                        {
                            Setters = {
                                new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center),
                                new Setter(TextBlock.MarginProperty, new Thickness(10, 0, 10, 0))
                            }
                        }
                    };
                }
            }

            ItemsGrid.Columns.Add(gridColumn);
        }

        if (ItemsGrid.Columns.Count > 0)
            ItemsGrid.Columns[^1].Width = new DataGridLength(1, DataGridLengthUnitType.Star);
    }

    // Ensures that the selected row is always scrolled into the visible area of the grid.
    private void ItemsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is DataGrid grid && grid.SelectedItem != null)
        {
            grid.ScrollIntoView(grid.SelectedItem);
        }
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {

    }
}