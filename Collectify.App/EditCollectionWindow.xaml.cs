using System.Windows;

namespace Collectify.App;

public partial class EditCollectionWindow : Window
{
    public string NewDescription => DescBox.Text;

    public EditCollectionWindow(string currentName, string? currentDescription)
    {
        InitializeComponent();
        NameBox.Text = currentName;
        DescBox.Text = currentDescription ?? string.Empty;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}