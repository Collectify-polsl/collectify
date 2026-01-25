using System.Windows;

namespace Collectify.App;

/// <summary>
/// Interaction logic for EditCollectionWindow.xaml.
/// </summary>
public partial class EditCollectionWindow : Window
{
    /// <summary>
    /// Gets the new description entered by the user.
    /// </summary>
    public string NewDescription => DescBox.Text;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditCollectionWindow"/> class.
    /// </summary>
    /// <param name="currentName">The current name of the collection.</param>
    /// <param name="currentDescription">The current description of the collection.</param>
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