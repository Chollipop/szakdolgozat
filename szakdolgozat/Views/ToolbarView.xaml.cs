using System.Windows.Controls;
using szakdolgozat.ViewModels;

namespace szakdolgozat.Views
{
    public partial class ToolbarView : UserControl
    {
        public ToolbarView()
        {
            InitializeComponent();
            this.DataContext = new ToolbarViewModel();
        }
    }
}
