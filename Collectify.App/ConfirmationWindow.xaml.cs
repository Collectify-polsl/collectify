using System.Windows;

namespace Collectify.App;

public partial class ConfirmationWindow : Window
{
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