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

public partial class ReferencePickerWindow : Window
{
    private readonly Dictionary<string, List<Item>> _map;
    private ICollectionView? _itemsView;

    // Właściwość, którą odczyta RowWizard po zamknięciu okna
    public int? SelectedItemId { get; private set; }

    public ReferencePickerWindow(Dictionary<string, List<Item>> map)
    {
        InitializeComponent();
        _map = map;

        // Załaduj listę nazw kolekcji do ComboBoxa
        CollectionCombo.ItemsSource = _map.Keys.OrderBy(k => k).ToList();

        // Opcjonalnie: wybierz pierwszą kolekcję na start
        if (_map.Keys.Any())
        {
            CollectionCombo.SelectedIndex = 0;
        }
    }

    private void CollectionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CollectionCombo.SelectedItem is string selectedCollection)
        {
            // Po zmianie kolekcji, wyświetl przedmioty do niej należące
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
            // Add more checks if needed, but Text is primary
        }
        
        return false;
    }

    private void Select_Click(object sender, RoutedEventArgs e)
    {
        // Sprawdź czy użytkownik coś wybrał
        if (ItemsList.SelectedItem is Item selectedItem)
        {
            SelectedItemId = selectedItem.Id;
            DialogResult = true; // Zamyka okno i wraca do RowWizard z wynikiem true
        }
        else
        {
            MessageBox.Show("Please select an item from the list first.",
                            "No selection", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}