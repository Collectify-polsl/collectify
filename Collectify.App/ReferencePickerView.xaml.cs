using Collectify.Model.Collection;
using Collectify.Model.Entities;
using System.Collections.Generic;
using System.Windows;

namespace Collectify.App;

public partial class ReferencePickerWindow : Window
{
    private readonly Dictionary<string, List<Item>> _map;
    public int? SelectedItemId { get; private set; }

    public ReferencePickerWindow(Dictionary<string, List<Item>> map)
    {
        InitializeComponent();
        _map = map;
        CollectionCombo.ItemsSource = _map.Keys;
    }

    private void CollectionCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (CollectionCombo.SelectedItem is string selectedCollection)
        {
            ItemsList.ItemsSource = _map[selectedCollection];
        }
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
            MessageBox.Show("Proszę wybrać przedmiot z listy.");
        }
    }
}