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
    }

    private async Task LoadCollectionsAsync()
    {
        Collections.Clear();
        var collections = await _unitOfWork.Collections.GetAllAsync();
        foreach (var c in collections)
            Collections.Add(c);
    }
}