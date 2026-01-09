using Collectify.Model.Collection;
using Collectify.Model.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Collectify.App;

public partial class ReferencePickerWindow : Window
{
    private readonly Dictionary<string, List<Item>> _map;

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
                ItemsList.ItemsSource = items;
            }
        }
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
            MessageBox.Show("Proszę najpierw wybrać przedmiot z listy.",
                            "Brak wyboru", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}