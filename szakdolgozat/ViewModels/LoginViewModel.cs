using Microsoft.Extensions.DependencyInjection;
using System.Windows.Input;
using szakdolgozat.Services;

namespace szakdolgozat.ViewModels
{
    public partial class LoginViewModel : BaseViewModel
    {
        private NavigationService<AssetListViewModel> navigationService;
        private bool loginSuccess;

        public ICommand LoginCommand { get; }
        public bool KeepMeLoggedIn { get; set; }
        
        public LoginViewModel()
        {
            navigationService = new NavigationService<AssetListViewModel>(() => App.ServiceProvider.GetRequiredService<AssetListViewModel>());
            LoginCommand = new RelayCommand(async () => await ExecuteLoginAsync());
        }

        private async Task ExecuteLoginAsync()
        {
            loginSuccess = await AuthenticationService.Instance.LoginAsync(KeepMeLoggedIn);

            if (loginSuccess)
            {
                navigationService.Navigate();
            }
        }
    }
}
