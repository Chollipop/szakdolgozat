using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;
using szakdolgozat.ViewModels;

namespace szakdolgozat.Components
{
    public partial class AssetFilter : UserControl
    {
        public AssetFilter()
        {
            InitializeComponent();
            this.DataContext = App.ServiceProvider.GetRequiredService<AssetFilterViewModel>();
        }
    }
}
