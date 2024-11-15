using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;
using szakdolgozat.ViewModels;

namespace szakdolgozat.Views
{
    public partial class AssetFilterView : UserControl
    {
        public AssetFilterView()
        {
            InitializeComponent();
            this.DataContext = App.ServiceProvider.GetRequiredService<AssetFilterViewModel>();
        }
    }
}
