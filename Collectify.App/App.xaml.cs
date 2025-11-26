using Collectify.App.ViewModels;
using Collectify.Data;
using Collectify.Data.Services;
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

            var context = DbFactory.CreateContext();
            context.Database.EnsureCreated();

            IUnitOfWork unitOfWork = new EfUnitOfWork(context);

            ICollectionService collectionService = new CollectionService(unitOfWork);
            ITemplateService templateService = new TemplateService(unitOfWork);

            var mainViewModel = new MainWindowViewModel(
                collectionService,
                createCollectionWindowFactory: () =>
                {
                    var newVm = new NewCollectionViewModel(collectionService, templateService);
                    return new NewCollectionView(newVm);
                }
            );

            MainWindow mainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };
            mainWindow.Show();
        }
    }
}