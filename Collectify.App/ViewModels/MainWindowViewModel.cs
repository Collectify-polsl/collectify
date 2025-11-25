using Collectify.App.Commands;
using Collectify.Model.Collection;
using Collectify.Model.Interfaces;
using System.Collections.ObjectModel;
using System.Windows.Input;


namespace Collectify.App.ViewModels;

public class MainWindowViewModel
{
    
    private readonly IUnitOfWork _unitOfWork;

    public ObservableCollection<Collection> Collections { get; } = new();

    public ICommand LoadCollectionsCommand { get; }

    public MainWindowViewModel(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        LoadCollectionsCommand = new AsyncRelayCommand(LoadCollectionsAsync);

        Collections.Add(new Collection
        {
            Name = "Stare Monety",
            Description = "5 rzymskich, 3 PRL",
            Items = new ObservableCollection<Item> { new Item(), new Item(), new Item() }
        });

        Collections.Add(new Collection
        {
            Name = "Karty Kolekcjonerskie",
            Description = "Kolekcja z lat 90-tych",
            Items = new ObservableCollection<Item> { new Item(), new Item(), new Item(), new Item(), new Item() }
        });

        Collections.Add(new Collection
        {
            Name = "Wina",
            Description = "Zbiór Bordeaux",
            Items = new ObservableCollection<Item> { new Item() }
        });
        Collections.Add(new Collection
        {
            Name = "Filmy Blu-ray",
            Description = "Klasyka Science Fiction",
            Items = new ObservableCollection<Item> { new Item(), new Item(), new Item(), new Item(), new Item(), new Item(), new Item() }
        });

        Collections.Add(new Collection
        {
            Name = "Autografy",
            Description = "Gwiazdy Rocka i Sportu",
            Items = new ObservableCollection<Item> { new Item(), new Item(), new Item(), new Item() }
        });

        Collections.Add(new Collection
        {
            Name = "Instrumenty Muzyczne",
            Description = "Gitary i Wzmacniacze",
            Items = new ObservableCollection<Item> { new Item(), new Item() }
        });

        Collections.Add(new Collection
        {
            Name = "Modele Samochodów",
            Description = "Skala 1:18, edycje limitowane",
            Items = new ObservableCollection<Item> { new Item(), new Item(), new Item(), new Item(), new Item(), new Item(), new Item(), new Item(), new Item(), new Item() }
        });
    }

    private async Task LoadCollectionsAsync()
    {
        Collections.Clear();
        var collections = await _unitOfWork.Collections.GetAllAsync();
        foreach (var c in collections)
            Collections.Add(c);
    }


}