using Microsoft.Extensions.DependencyInjection;
using szakdolgozat.Stores;
using szakdolgozat.ViewModels;

namespace szakdolgozat.Services
{
    public class NavigationService<TViewModel> : INavigationService where TViewModel : BaseViewModel
    {
        private readonly NavigationStore _navigationStore;
        private readonly Func<TViewModel> _createViewModel;

        public NavigationService(Func<TViewModel> createViewModel)
        {
            _navigationStore = App.ServiceProvider.GetRequiredService<NavigationStore>();
            _createViewModel = createViewModel;
        }

        public void Navigate()
        {
            _navigationStore.CurrentViewModel = _createViewModel();
        }
    }
}
