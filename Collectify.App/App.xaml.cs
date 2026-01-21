using Collectify.App.ViewModels;
using Collectify.Data;
using Collectify.Data.Services;
using Collectify.Model.Interfaces;
using System.Linq;
using System.Windows;

namespace Collectify.App
{
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