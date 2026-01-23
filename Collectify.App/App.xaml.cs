using Collectify.App.ViewModels;
using Collectify.Data;
using Collectify.Data.Services;
using Collectify.Model.Interfaces;
using System.Linq;
using System.Windows;

namespace Collectify.App
{
    // Serves as the application entry point, handling global lifecycle events and dependency injection.
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        // Bootstraps the application by initializing the database, core services, and the main window with its dependencies.
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
                // Defines the factory logic for creating the new collection window and its associated view model.
                createCollectionWindowFactory: () =>
                {
                    var vm = new NewCollectionViewModel(collectionService, templateService);
                    var view = new NewCollectionView(vm);
                    vm.CloseAction = view.Close;
                    return view;
                },
                // Configures the row wizard factory, including recursive navigation logic for jumping between item editors.
                rowWizardWindowFactory: (collection, item) =>
                {
                    var vm = new RowWizardViewModel(collection, itemService, templateService, collectionService, item);
                    var view = new RowWizardView(vm);

                    vm.RequestNavigateToItemId = itemId =>
                    {
                        var nextItem = itemService
                            .GetItemAsync(itemId, includeFieldValues: true)
                            .GetAwaiter()
                            .GetResult();

                        if (nextItem == null)
                            return;

                        var nextVm = new RowWizardViewModel(collection, itemService, templateService, collectionService, nextItem);
                        var nextView = new RowWizardView(nextVm)
                        {
                            Owner = view.Owner ?? Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive),
                            Title = "Edit Item"
                        };

                        nextVm.RequestNavigateToItemId = vm.RequestNavigateToItemId;
                        nextVm.CloseAction = nextView.Close;

                        nextView.ShowDialog();
                    };

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