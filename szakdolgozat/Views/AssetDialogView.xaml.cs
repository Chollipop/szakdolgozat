using System.Windows;
using szakdolgozat.ViewModels;

namespace szakdolgozat.Views
{
    public partial class AssetDialogView : Window
    {
        public AssetDialogView(AssetDialogViewModel assetDialogViewModel)
        {
            InitializeComponent();
            this.DataContext = assetDialogViewModel;
        }
    }
}
