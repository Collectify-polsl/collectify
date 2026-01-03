using System.Windows;
using Collectify.App.ViewModels;

namespace Collectify.App.Views;

public partial class EditTemplateView : Window
{
    public EditTemplateView(EditTemplateViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.CloseAction = Close;
    }
}