using System.Windows;
using szakdolgozat.ViewModels;

namespace szakdolgozat.Views
{
    public partial class SubtypeDialogView : Window
    {
        public SubtypeDialogView(SubtypeDialogViewModel subtypeDialogViewModel)
        {
            InitializeComponent();
            this.DataContext = subtypeDialogViewModel;
        }
    }
}
