using Collectify.App.Commands;
using Collectify.App.Views;
using Collectify.Data.Services;
using System.Windows.Controls;
using Collectify.Model.Collection;
using Collectify.Model.Interfaces;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace Collectify.App.ViewModels;

public class MainWindowViewModel: INotifyPropertyChanged
{
    private readonly ICollectionService _collectionService;
    private readonly IItemService _itemService;
    private readonly ITemplateService _templateService;


    private readonly Func<Window> _createCollectionWindowFactory;
    private readonly Func<Collection, Item?, Window> _rowWizardWindowFactory;

    private SingleCollectionViewModel? _activeDetailsViewModel;
    public SingleCollectionViewModel? ActiveDetailsViewModel
    {
        get => _activeDetailsViewModel;
        set
        {
            _activeDetailsViewModel = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsDetailsViewVisible));
        }
    }
    public bool IsDetailsViewVisible => ActiveDetailsViewModel != null;
    public ObservableCollection<Collection> Collections { get; } = new();

    public ICommand LoadCollectionsCommand { get; }
    public ICommand CreateCollectionCommand { get; }
    public ICommand OpenCollectionCommand { get; }

    public ICommand CreateTemplateCommand { get; }
    public ICommand EditTemplateCommand { get; }



    public MainWindowViewModel(
        ICollectionService collectionService,
        IItemService itemService,
        ITemplateService templateService,
        Func<Window> createCollectionWindowFactory,
        Func<Collection, Item?, Window> rowWizardWindowFactory
        )

    {
        _collectionService = collectionService;
        _itemService = itemService;
        _templateService = templateService;
        _createCollectionWindowFactory = createCollectionWindowFactory;
        _rowWizardWindowFactory = rowWizardWindowFactory;

        LoadCollectionsCommand = new AsyncRelayCommand(LoadCollectionsAsync);
        CreateCollectionCommand = new RelayCommand(OpenCreateCollectionWindow);
        OpenCollectionCommand = new RelayCommand<Collection>(OpenDetailsView);
        CreateTemplateCommand = new RelayCommand(OpenNewTemplateWindow);
        EditTemplateCommand = new RelayCommand(OpenEditTemplateWindow);
        LoadCollectionsAsync();
    }
    private void OpenCreateCollectionWindow()
    {
        var window = _createCollectionWindowFactory();
        window.Owner = Application.Current.MainWindow;

        bool? result = window.ShowDialog();

        LoadCollectionsAsync();
    }
    private void OpenNewTemplateWindow()
    {
        var vm = new NewTemplateViewModel(_templateService);
        var view = new NewTemplateView(vm);
        vm.CloseAction = view.Close;
        view.Owner = Application.Current.MainWindow;
        view.ShowDialog();
    }
    private void OpenEditTemplateWindow()
    {
        var vm = new EditTemplateViewModel(_templateService);
        var view = new EditTemplateView(vm);
        vm.CloseAction = view.Close;

        view.Owner = Application.Current.MainWindow;
        view.ShowDialog();
    }
    private void OpenDetailsView(Collection collection)
    {
        if (collection == null) return;

        var detailsVM = new SingleCollectionViewModel(collection, _itemService, _templateService, _collectionService, _rowWizardWindowFactory);

        detailsVM.NavigateBackAction = () =>
        {
            ActiveDetailsViewModel = null;
            LoadCollectionsAsync();
        };
        detailsVM.SwitchCollectionAction = async (newCollectionId) =>
        {
            var allCollections = await _collectionService.GetCollectionsAsync();

            var newCollection = allCollections.FirstOrDefault(c => c.Id == newCollectionId);

            if (newCollection != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => OpenDetailsView(newCollection));
            }
        };

        ActiveDetailsViewModel = detailsVM;
    }

    private async Task LoadCollectionsAsync()
    {
        Collections.Clear();
        var collections = await _collectionService.GetCollectionsAsync();
        foreach (var c in collections) Collections.Add(c);
    }
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}