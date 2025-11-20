using Collectify.App.ViewModels;
using Collectify.Data;
using Collectify.Model.Interfaces;
using System.Windows;

namespace Collectify.App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            CollectifyContext context = DbFactory.CreateContext();
            IUnitOfWork unitOfWork = new EfUnitOfWork(context);

            MainWindowViewModel mainViewModel = new MainWindowViewModel(unitOfWork);

            MainWindow mainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };
            mainWindow.Show();
        }
    }
}