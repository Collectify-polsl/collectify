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

/// <summary>
/// The main view model for the application, managing the list of collections and navigation.
/// </summary>
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

    /// <summary>
    /// Gets the list of available collections.
    /// </summary>
    public ObservableCollection<Collection> Collections { get; } = new();

    /// <summary>
    /// Command to load collections from the data source.
    /// </summary>
    public ICommand LoadCollectionsCommand { get; }

    /// <summary>
    /// Command to open the window for creating a new collection.
    /// </summary>
    public ICommand CreateCollectionCommand { get; }

    /// <summary>
    /// Command to open the details view for a specific collection.
    /// </summary>
    public ICommand OpenCollectionCommand { get; }

    /// <summary>
    /// Command to open the window for creating a new template.
    /// </summary>
    public ICommand CreateTemplateCommand { get; }

    /// <summary>
    /// Command to open the window for editing an existing template.
    /// </summary>
    public ICommand EditTemplateCommand { get; }



    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="collectionService">The service for managing collections.</param>
    /// <param name="itemService">The service for managing items.</param>
    /// <param name="templateService">The service for managing templates.</param>
    /// <param name="createCollectionWindowFactory">Factory function to create the new collection window.</param>
    /// <param name="rowWizardWindowFactory">Factory function to create the row wizard window.</param>
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