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
        public App()
        {
            InitializeComponent();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var context = DbFactory.CreateContext();
            context.Database.EnsureCreated();

            IUnitOfWork unitOfWork = new EfUnitOfWork(context);

            ICollectionService collectionService = new CollectionService(unitOfWork);
            ITemplateService templateService = new TemplateService(unitOfWork);
            IItemService itemService = new ItemService(unitOfWork);

            var mainViewModel = new MainWindowViewModel(
                collectionService,
                itemService,
                templateService,
                createCollectionWindowFactory: () =>
                {
                    var vm = new NewCollectionViewModel(collectionService, templateService);
                    var view = new NewCollectionView(vm);
                    vm.CloseAction = view.Close;
                    return view;
                },
                rowWizardWindowFactory: (collection, item) =>
                {
                    var vm = new RowWizardViewModel(collection, itemService, templateService, collectionService, item);
                    var view = new RowWizardView(vm);
                    vm.CloseAction = view.Close;
                    return view;
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