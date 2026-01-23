using Collectify.App.Commands;
using Collectify.App.ViewModels;
using Collectify.Model.Collection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
namespace Collectify.App
{

    public partial class NewCollectionView : Window
    {


        public NewCollectionView(NewCollectionViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            viewModel.CloseAction = () => this.Close();
        }


    }
}
