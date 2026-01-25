using System.Windows;
using Collectify.App.ViewModels;

namespace Collectify.App.Views
{
    /// <summary>
    /// Interaction logic for NewTemplateView.xaml.
    /// </summary>
    public partial class NewTemplateView : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NewTemplateView"/> class.
        /// </summary>
        /// <param name="vm">The view model.</param>
        public NewTemplateView(NewTemplateViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            vm.CloseAction = Close;
        }
    }
}
