using Microsoft.Extensions.DependencyInjection;
using System.Windows.Input;
using szakdolgozat.Services;

namespace szakdolgozat.ViewModels
{
    public class ToolbarViewModel : BaseViewModel
    {
        private INavigationService navigationService;
        public ICommand ViewAssetsCommand { get; }
        public ICommand ViewAssetAssignmentsCommand { get; }
        public ICommand ViewAssetLogsCommand { get; }
        public ICommand LogoutCommand { get; }

        public string CurrentUser => $"Current User: {AuthenticationService.Instance.CurrentUser.Username}";

        public ToolbarViewModel()
        {
            ViewAssetsCommand = new RelayCommand(ViewAssets);
            ViewAssetAssignmentsCommand = new RelayCommand(ViewAssetAssignments);
            ViewAssetLogsCommand = new RelayCommand(ViewAssetLogs);
            LogoutCommand = new RelayCommand(Logout);
        }

        private void ViewAssets()
        {
            navigationService = new NavigationService<AssetListViewModel>(() => App.ServiceProvider.GetRequiredService<AssetListViewModel>());
            navigationService.Navigate();
        }

        private void ViewAssetAssignments()
        {
            navigationService = new NavigationService<AssetAssignmentListViewModel>(() => App.ServiceProvider.GetRequiredService<AssetAssignmentListViewModel>());
            navigationService.Navigate();
        }

        private void ViewAssetLogs()
        {
            navigationService = new NavigationService<AssetLogViewModel>(() => App.ServiceProvider.GetRequiredService<AssetLogViewModel>());
            navigationService.Navigate();
        }

        private void Logout()
        {
            AuthenticationService.Instance.Logout();
            navigationService = new NavigationService<LoginViewModel>(() => App.ServiceProvider.GetRequiredService<LoginViewModel>());
            navigationService.Navigate();
        }
    }
}
