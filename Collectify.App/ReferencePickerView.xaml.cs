using Collectify.Model.Collection;
using Collectify.Model.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Collectify.App;

/// <summary>
/// Interaction logic for ReferencePickerWindow.xaml.
/// </summary>
public partial class ReferencePickerWindow : Window
{
    private readonly Dictionary<string, List<Item>> _map;
    private ICollectionView? _itemsView;

    /// <summary>
    /// Gets the ID of the selected item.
    /// </summary>
    public int? SelectedItemId { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReferencePickerWindow"/> class.
    /// </summary>
    /// <param name="map">A dictionary mapping collection names to lists of items.</param>
    public ReferencePickerWindow(Dictionary<string, List<Item>> map)
    {
        InitializeComponent();
        _map = map;

        // Load collection names into the ComboBox
        CollectionCombo.ItemsSource = _map.Keys.OrderBy(k => k).ToList();

        // Optionally select the first collection
        if (_map.Keys.Any())
        {
            CollectionCombo.SelectedIndex = 0;
        }
    }

    private void CollectionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CollectionCombo.SelectedItem is string selectedCollection)
        {
            // Update the items list when the collection selection changes
            if (_map.TryGetValue(selectedCollection, out var items))
            {
                _itemsView = CollectionViewSource.GetDefaultView(items);
                _itemsView.Filter = FilterItem;
                ItemsList.ItemsSource = _itemsView;
                
                // Re-apply filter if text exists
                _itemsView.Refresh();
            }
        }
    }

    private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _itemsView?.Refresh();
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
}