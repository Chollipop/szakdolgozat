using Microsoft.Extensions.DependencyInjection;
using szakdolgozat.Stores;

namespace szakdolgozat.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        private readonly NavigationStore _navigationStore;
        private string _windowTitle;

        public BaseViewModel CurrentViewModel => _navigationStore.CurrentViewModel;

        public string WindowTitle
        {
            get => _windowTitle;
            set
            {
                _windowTitle = value;
                OnPropertyChanged(nameof(WindowTitle));
            }
        }

        public MainWindowViewModel()
        {
            _navigationStore = App.ServiceProvider.GetRequiredService<NavigationStore>();
            _navigationStore.CurrentViewModelChanged += OnCurrentViewModelChanged;
            UpdateWindowTitle();
        }

        private void OnCurrentViewModelChanged()
        {
            OnPropertyChanged(nameof(CurrentViewModel));
            UpdateWindowTitle();
        }

        private void UpdateWindowTitle()
        {
            if (CurrentViewModel is LoginViewModel)
            {
                WindowTitle = "Login";
            }
            else if (CurrentViewModel is AssetListViewModel)
            {
                WindowTitle = "Assets";
            }
            else if (CurrentViewModel is AssetAssignmentListViewModel)
            {
                WindowTitle = "Asset Assignments";
            }
        }
    }
}
