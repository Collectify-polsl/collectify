using Collectify.Model.Collection;
using Collectify.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Collectify.App;

public partial class ReferencePickerWindow : Window
{
    private readonly Dictionary<string, List<Item>> _map;
    private readonly bool _allowMultiple;
    private readonly HashSet<int> _preselectedIds;

    // Właściwość, którą odczyta RowWizard po zamknięciu okna
    public int? SelectedItemId { get; private set; }
    public IReadOnlyList<int> SelectedItemIds { get; private set; } = Array.Empty<int>();

    public ReferencePickerWindow(
        Dictionary<string, List<Item>> map,
        bool allowMultiple,
        IEnumerable<int>? preselectedIds = null)
    {
        InitializeComponent();

        _map = map;
        _allowMultiple = allowMultiple;
        _preselectedIds = preselectedIds is null ? new HashSet<int>() : new HashSet<int>(preselectedIds);

        ItemsList.SelectionMode = _allowMultiple ? SelectionMode.Extended : SelectionMode.Single;
        SelectionHint.Text = _allowMultiple
            ? "Tip: hold Ctrl or Shift to select multiple entries."
            : "Select a single item and click Accept.";

        // Załaduj listę nazw kolekcji do ComboBoxa
        CollectionCombo.ItemsSource = _map.Keys.OrderBy(k => k).ToList();

        // Opcjonalnie: wybierz pierwszą kolekcję na start
        if (_map.Count > 0)
            CollectionCombo.SelectedIndex = 0;
    }

    private void CollectionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CollectionCombo.SelectedItem is not string selectedCollection)
            return;

        // Po zmianie kolekcji, wyświetl przedmioty do niej należące
        if (!_map.TryGetValue(selectedCollection, out var items))
            return;

        ItemsList.ItemsSource = items;

        if (!_preselectedIds.Any())
            return;

        if (_allowMultiple)
        {
            foreach (var item in items.Where(i => _preselectedIds.Contains(i.Id)))
                ItemsList.SelectedItems.Add(item);
        }
        else
        {
            var first = items.FirstOrDefault(i => _preselectedIds.Contains(i.Id));
            if (first != null)
                ItemsList.SelectedItem = first;
        }
    }

    private void Select_Click(object sender, RoutedEventArgs e)
    {
        if (_allowMultiple)
        {
            var ids = ItemsList.SelectedItems.Cast<Item>().Select(i => i.Id).Distinct().ToList();
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

        // Sprawdź czy użytkownik coś wybrał
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