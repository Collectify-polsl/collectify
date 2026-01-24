using Collectify.Model.Collection;
using Collectify.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Collectify.App;

// Provides a modal interface for searching and selecting item references across different collections.
public partial class ReferencePickerWindow : Window
{
    private readonly Dictionary<string, List<Item>> _map;
    private readonly bool _allowMultiple;
    private readonly HashSet<int> _selectedIds; // accumulates picks across collections

    public int? SelectedItemId { get; private set; }
    public IReadOnlyList<int> SelectedItemIds { get; private set; } = Array.Empty<int>();

    // Initializes the window with collection data and configures single or multiple selection modes.
    public ReferencePickerWindow(
        Dictionary<string, List<Item>> map,
        bool allowMultiple,
        IEnumerable<int>? preselectedIds = null)
    {
        InitializeComponent();

        _map = map;
        _allowMultiple = allowMultiple;
        _selectedIds = preselectedIds is null ? new HashSet<int>() : new HashSet<int>(preselectedIds);

        ItemsList.SelectionMode = _allowMultiple ? SelectionMode.Extended : SelectionMode.Single;
        SelectionHint.Text = _allowMultiple
            ? "Hold Ctrl or Shift to select multiple entries."
            : "Select a single item and click Accept.";

        CollectionCombo.ItemsSource = _map.Keys.OrderBy(k => k).ToList();

        if (_map.Count > 0)
            CollectionCombo.SelectedIndex = 0;
    }

    // Filters and displays items belonging to the selected collection while maintaining previous selections.
    private void CollectionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Persist selections from the previously shown collection
        if (_allowMultiple)
        {
            foreach (var picked in ItemsList.SelectedItems.Cast<Item>())
                _selectedIds.Add(picked.Id);
        }

        if (CollectionCombo.SelectedItem is not string selectedCollection)
            return;

        if (!_map.TryGetValue(selectedCollection, out var items))
            return;

        ItemsList.ItemsSource = items;

        // Re-apply any previously chosen IDs that belong to this collection
        if (_selectedIds.Any())
        {
            if (_allowMultiple)
            {
                foreach (var item in items.Where(i => _selectedIds.Contains(i.Id)))
                    ItemsList.SelectedItems.Add(item);
            }
            else
            {
                var first = items.FirstOrDefault(i => _selectedIds.Contains(i.Id));
                if (first != null)
                    ItemsList.SelectedItem = first;
            }
        }
    }

    // Validates the current selection and returns the chosen item IDs to the calling wizard.
    private void Select_Click(object sender, RoutedEventArgs e)
    {
        if (_allowMultiple)
        {
            // Merge current view selection into the accumulated set
            foreach (var picked in ItemsList.SelectedItems.Cast<Item>())
                _selectedIds.Add(picked.Id);

            var ids = _selectedIds.ToList();
            if (ids.Count == 0)
            {
                MessageBox.Show("Please select at least one item.", "No selection", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SelectedItemIds = ids;
            SelectedItemId = null;
            DialogResult = true;
            return;
        }

        if (ItemsList.SelectedItem is Item selected)
        {
            SelectedItemId = selected.Id;
            SelectedItemIds = new[] { selected.Id };
            DialogResult = true;
        }
        else
        {
            MessageBox.Show("Please select an item from the list first.",
                            "No selection", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}