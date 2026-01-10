using System.Windows;
using Collectify.App.ViewModels;

namespace Collectify.App.Views;

public partial class EditTemplateView : Window
{
    public EditTemplateView(EditTemplateViewModel viewModel)
    {
        InitializeComponent();
        this.DataContext = viewModel;

        viewModel.CloseAction ??= () => this.Close();
    }
}