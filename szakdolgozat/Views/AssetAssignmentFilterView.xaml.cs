using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;
using szakdolgozat.ViewModels;

namespace szakdolgozat.Views
{
    public partial class AssetAssignmentFilterView : UserControl
    {
        public AssetAssignmentFilterView()
        {
            InitializeComponent();
            this.DataContext = App.ServiceProvider.GetRequiredService<AssetAssignmentFilterViewModel>();
        }
    }
}
