using System.Windows.Controls;
using szakdolgozat.ViewModels;

namespace szakdolgozat.Components
{
    public partial class Toolbar : UserControl
    {
        public Toolbar()
        {
            InitializeComponent();
            this.DataContext = new ToolbarViewModel();
        }
    }
}
