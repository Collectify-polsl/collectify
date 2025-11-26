using Collectify.App.Commands;
using Collectify.Model.Collection;
using Collectify.Model.Interfaces;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace Collectify.App.ViewModels;

public class MainWindowViewModel
{
    private readonly ICollectionService _collectionService;
    private readonly Func<Window> _createCollectionWindowFactory; // <--- FABRYKA

    public ObservableCollection<Collection> Collections { get; } = new();

    public ICommand LoadCollectionsCommand { get; }
    public ICommand CreateCollectionCommand { get; } // <--- NOWA KOMENDA

    // Dodajemy factory do konstruktora
    public MainWindowViewModel(ICollectionService collectionService, Func<Window> createCollectionWindowFactory)
    {
        _collectionService = collectionService;
        _createCollectionWindowFactory = createCollectionWindowFactory;

        LoadCollectionsCommand = new AsyncRelayCommand(LoadCollectionsAsync);
        CreateCollectionCommand = new RelayCommand(OpenCreateCollectionWindow);

        LoadCollectionsAsync();
    }

    private void OpenCreateCollectionWindow()
    {
        // 1. Stwórz okno używając fabryki (wszystkie zależności są wstrzykiwane w App.xaml.cs)
        var window = _createCollectionWindowFactory();

        // 2. Otwórz jako Dialog (blokuje główne okno)
        bool? result = window.ShowDialog();

        // 3. Po zamknięciu odśwież listę
        LoadCollectionsAsync();
    }

    private async Task LoadCollectionsAsync()
    {
        Collections.Clear();
        var collections = await _collectionService.GetCollectionsAsync();
        foreach (var c in collections) Collections.Add(c);
    }
}