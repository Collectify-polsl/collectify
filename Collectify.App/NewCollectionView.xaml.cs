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

    /// <summary>
    /// Interaction logic for NewCollectionView.xaml.
    /// </summary>
    public partial class NewCollectionView : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NewCollectionView"/> class.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        public NewCollectionView(NewCollectionViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            viewModel.CloseAction = () => this.Close();
        }
    }
}
