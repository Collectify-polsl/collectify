using System.Collections.ObjectModel;
using System.Windows;

namespace Collectify.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        public ObservableCollection<TileModel> TileCollections { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            TileCollections = new ObservableCollection<TileModel>
            {
                new TileModel { Title = "Książki do przeczytania"},
                new TileModel { Title = "Filmy do obejrzenia"},
                new TileModel { Title = "Projekty WPF"}

            };
        }


        private void newCollButtonClick(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("nacisniete");
        }
    }
}