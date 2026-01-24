using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Collectify.App;

public partial class ReferencePreviewWindow : Window
{
    private readonly List<Collectify.Model.Collection.Item> _items;
    private readonly List<int> _removed = new();

    public bool AllowRemove { get; }
    public IReadOnlyList<int> RemovedItemIds => _removed;

    public ReferencePreviewWindow()
        : this(Enumerable.Empty<Collectify.Model.Collection.Item>(), allowRemove: false)
    {
    }

    public ReferencePreviewWindow(IEnumerable<Collectify.Model.Collection.Item> items, bool allowRemove = false)
    {
        AllowRemove = allowRemove;
        InitializeComponent();
        _items = items?.ToList() ?? new List<Collectify.Model.Collection.Item>();
        ItemsList.ItemsSource = _items;
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (!AllowRemove)
            return;

        if ((sender as Button)?.CommandParameter is not Collectify.Model.Collection.Item item)
            return;

        _removed.Add(item.Id);
        _items.Remove(item);
        ItemsList.ItemsSource = null;
        ItemsList.ItemsSource = _items;
    }
}