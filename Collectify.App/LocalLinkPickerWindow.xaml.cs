using Collectify.Model.Collection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Collectify.App;

/// <summary>
/// Interaction logic for LocalLinkPickerWindow.xaml.
/// </summary>
public partial class LocalLinkPickerWindow : Window
{
    private ICollectionView _itemsView;
    
    /// <summary>
    /// Gets the ID of the selected item.
    /// </summary>
    public int? SelectedItemId { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalLinkPickerWindow"/> class.
    /// </summary>
    /// <param name="items">The list of items to display.</param>
    /// <param name="initialSelectedId">The initially selected item ID.</param>
    public LocalLinkPickerWindow(List<Item> items, int? initialSelectedId)
    {
        InitializeComponent();
        
        _itemsView = CollectionViewSource.GetDefaultView(items);
        _itemsView.Filter = FilterItem;
        ItemsList.ItemsSource = _itemsView;
        
        if (initialSelectedId.HasValue)
        {
            var item = items.FirstOrDefault(i => i.Id == initialSelectedId.Value);
            if (item != null)
            {
                ItemsList.SelectedItem = item;
                ItemsList.ScrollIntoView(item);
            }
        }
    }

    private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _itemsView.Refresh();
    }

    private bool FilterItem(object obj)
    {
        if (obj is not Item item) return false;
        
        string filterText = FilterTextBox.Text;
        if (string.IsNullOrWhiteSpace(filterText)) return true;

        foreach (var val in item.FieldValues)
        {
            if (val.TextValue != null && val.TextValue.Contains(filterText, StringComparison.OrdinalIgnoreCase)) return true;
            if (val.IntValue.HasValue && val.IntValue.ToString().Contains(filterText)) return true;
        }
        
        return false;
    }

    private void Select_Click(object sender, RoutedEventArgs e)
    {
        if (ItemsList.SelectedItem is Item selectedItem)
        {
            SelectedItemId = selectedItem.Id;
            DialogResult = true;
        }
        else
        {
            MessageBox.Show("Please select an item from the list first.",
                            "No selection", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        SelectedItemId = null;
        DialogResult = true;
    }
}