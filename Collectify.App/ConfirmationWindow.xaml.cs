using System.Windows;

namespace Collectify.App;

/// <summary>
/// Interaction logic for ConfirmationWindow.xaml.
/// </summary>
public partial class ConfirmationWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfirmationWindow"/> class.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="title">The title of the window.</param>
    public ConfirmationWindow(string message, string title = "Confirm")
    {
        InitializeComponent();
        MessageText.Text = message;
        Title = title;
    }

    private void Yes_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void No_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}