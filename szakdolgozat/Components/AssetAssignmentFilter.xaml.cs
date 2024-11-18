using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;
using szakdolgozat.ViewModels;

namespace szakdolgozat.Components
{
    public partial class AssetAssignmentFilter : UserControl
    {
        public AssetAssignmentFilter()
        {
            InitializeComponent();
            this.DataContext = App.ServiceProvider.GetRequiredService<AssetAssignmentFilterViewModel>();
        }
    }
}
