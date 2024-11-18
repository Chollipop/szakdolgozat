using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;
using szakdolgozat.ViewModels;

namespace szakdolgozat.Components
{
    public partial class AssetLogFilter : UserControl
    {
        public AssetLogFilter()
        {
            InitializeComponent();
            this.DataContext = App.ServiceProvider.GetRequiredService<AssetLogFilterViewModel>();
        }
    }
}
