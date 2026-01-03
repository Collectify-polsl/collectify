using System.Windows;
using Collectify.App.ViewModels;

namespace Collectify.App.Views
{
    public partial class NewTemplateView : Window
    {
        public NewTemplateView(NewTemplateViewModel vm)
        {
            InitializeComponent(); // <-- TERAZ ZADZIAŁA
            DataContext = vm;
            vm.CloseAction = Close;
        }
    }
}
