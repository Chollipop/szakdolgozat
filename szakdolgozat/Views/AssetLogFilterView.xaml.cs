using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;
using szakdolgozat.ViewModels;

namespace szakdolgozat.Views
{
    public partial class AssetLogFilterView : UserControl
    {
        public AssetLogFilterView()
        {
            InitializeComponent();
            this.DataContext = App.ServiceProvider.GetRequiredService<AssetLogFilterViewModel>();
        }
    }
}
