using System.Windows;
using Collectify.App.ViewModels;

namespace Collectify.App.Views;

/// <summary>
/// Interaction logic for EditTemplateView.xaml.
/// </summary>
public partial class EditTemplateView : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EditTemplateView"/> class.
    /// </summary>
    /// <param name="viewModel">The view model.</param>
    public EditTemplateView(EditTemplateViewModel viewModel)
    {
        InitializeComponent();
        this.DataContext = viewModel;

        viewModel.CloseAction ??= () => this.Close();
    }
}