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
    // Represents the modal window for creating a new collection, linking the UI to the creation logic.
    public partial class NewCollectionView : Window
    {
        // Initializes the creation dialog and binds the ViewModel's close request to the window's close method.
        public NewCollectionView(NewCollectionViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            viewModel.CloseAction = () => this.Close();
        }
    }
}