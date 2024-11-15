using System.Windows;
using szakdolgozat.ViewModels;

namespace szakdolgozat.Views
{
    public partial class AssetAssignmentDialogView : Window
    {
        public AssetAssignmentDialogView(AssetAssignmentDialogViewModel assetAssignmentDialogViewModel)
        {
            InitializeComponent();
            this.DataContext = assetAssignmentDialogViewModel;
        }
    }
}
