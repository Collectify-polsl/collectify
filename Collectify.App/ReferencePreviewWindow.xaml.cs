using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Collectify.App;

public partial class ReferencePreviewWindow : Window
{
    public ReferencePreviewWindow()
        : this(Enumerable.Empty<Collectify.Model.Collection.Item>())
    {
    }

    public ReferencePreviewWindow(IEnumerable<Collectify.Model.Collection.Item> items)
    {
        InitializeComponent();
        ItemsList.ItemsSource = items?.ToList() ?? new List<Collectify.Model.Collection.Item>();
    }
}