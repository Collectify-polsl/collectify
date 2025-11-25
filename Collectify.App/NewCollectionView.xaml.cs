using Collectify.App.Commands;
using Collectify.Model.Collection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Collectify.App
{

    public partial class NewCollectionView : Window
    {
        

        public NewCollectionView(ObservableCollection<Collection> mainCollections)
        {
            InitializeComponent();
        
        }

     
    }
}
