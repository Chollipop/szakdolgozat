using szakdolgozat.ViewModels;

namespace szakdolgozat.Stores
{
    public class NavigationStore
    {
        private BaseViewModel _currentViewModel;
        public BaseViewModel CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                _currentViewModel?.Dispose();
                _currentViewModel = value;
                OnCurrentViewModelChanged();
            }
        }

        public event Action CurrentViewModelChanged;

        private void OnCurrentViewModelChanged()
        {
            CurrentViewModelChanged?.Invoke();
        }
    }
}
