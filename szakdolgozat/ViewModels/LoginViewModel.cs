using Microsoft.Extensions.DependencyInjection;
using System.Windows.Input;
using szakdolgozat.Services;

namespace szakdolgozat.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private NavigationService<AssetListViewModel> navigationService;
        private bool loginSuccess;

        public bool KeepMeLoggedIn { get; set; }

        public ICommand LoginCommand { get; }

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
                using (var scope = App.ServiceProvider.CreateScope())
                {
                    bool canConnect = false;
                    while (!canConnect)
                    {
                        canConnect = scope.ServiceProvider.GetRequiredService<AssetDbContext>().Database.CanConnect();
                        if (!canConnect)
                        {
                            Thread.Sleep(1000);
                        }
                    }
                }

                navigationService.Navigate();
            }
        }
    }
}
