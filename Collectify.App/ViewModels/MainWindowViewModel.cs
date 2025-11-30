using Collectify.App.Commands;
using Collectify.Data.Services;
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
    private readonly Func<Collection, Window> _rowWizardWindowFactory;

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

    public MainWindowViewModel(
        ICollectionService collectionService,
        IItemService itemService,
        ITemplateService templateService,
        Func<Window> createCollectionWindowFactory,
        Func<Collection, Window> rowWizardWindowFactory
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

        LoadCollectionsAsync();
    }

    private void OpenCreateCollectionWindow()
    {
        var window = _createCollectionWindowFactory();

        bool? result = window.ShowDialog();

        LoadCollectionsAsync();
    }
    private void OpenDetailsView(Collection collection)
    {
        if (collection == null) return;

        var detailsVM = new SingleCollectionViewModel(collection, _itemService, _templateService, _rowWizardWindowFactory);

        detailsVM.NavigateBackAction = () =>
        {
            ActiveDetailsViewModel = null;
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