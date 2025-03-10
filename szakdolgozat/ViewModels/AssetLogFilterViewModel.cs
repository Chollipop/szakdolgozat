using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Windows.Input;
using szakdolgozat.Models;
using szakdolgozat.Services;

namespace szakdolgozat.ViewModels
{
    public class AssetLogFilterViewModel : BaseViewModel
    {
        private AssetLogListViewModel _assetLogViewModel;

        private string _assetName;
        private string _selectedAction;
        private UserProfile _selectedUser;
        private DateTime? _timestampFrom;
        private DateTime? _timestampTo;

        public string AssetName
        {
            get => _assetName;
            set
            {
                _assetName = value;
                OnPropertyChanged(nameof(AssetName));
            }
        }

        public ObservableCollection<string> Actions { get; } = new ObservableCollection<string> { "", "Added", "Updated", "Deleted" };
        public string SelectedAction
        {
            get => _selectedAction;
            set
            {
                _selectedAction = value;
                OnPropertyChanged(nameof(SelectedAction));
            }
        }

        public ObservableCollection<UserProfile> Users { get; set; } = new ObservableCollection<UserProfile>();
        public UserProfile SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;
                OnPropertyChanged(nameof(SelectedUser));
            }
        }

        public DateTime? TimestampFrom
        {
            get => _timestampFrom;
            set
            {
                _timestampFrom = value;
                OnPropertyChanged(nameof(TimestampFrom));
            }
        }

        public DateTime? TimestampTo
        {
            get => _timestampTo;
            set
            {
                _timestampTo = value;
                OnPropertyChanged(nameof(TimestampTo));
            }
        }

        public ICommand ApplyFilterCommand { get; }
        public ICommand ClearFilterCommand { get; }

        public AssetLogFilterViewModel()
        {
            _assetLogViewModel = App.ServiceProvider.GetRequiredService<AssetLogListViewModel>();
            ApplyFilterCommand = new RelayCommand(ApplyFilter);
            ClearFilterCommand = new RelayCommand(() => ClearFilters());

            _assetLogViewModel.AssetLogsChangedReapplyFilters += OnAssetLogsChanged;
        }

        private async void LoadUsersAsync()
        {
            var users = await AuthenticationService.Instance.GetAllUsersAsync();
            foreach (var user in users)
            {
                Users.Add(user);
            }
        }

        private void OnAssetLogsChanged(object sender, EventArgs e)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            List<AssetLog> filteredLogs;
            using (var scope = App.ServiceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AssetDbContext>();
                var logsList = context.AssetLogs
                    .Include(l => l.Asset)
                    .Select(l => new AssetLog
                    {
                        LogID = l.LogID,
                        AssetID = l.AssetID,
                        Asset = l.Asset,
                        Action = l.Action,
                        Timestamp = l.Timestamp,
                        PerformedBy = l.PerformedBy
                    })
                    .ToList();
                filteredLogs = new List<AssetLog>(logsList);
            }

            if (!string.IsNullOrEmpty(AssetName))
            {
                filteredLogs = filteredLogs.Where(l => l.Asset.AssetName.Contains(AssetName, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrEmpty(SelectedAction))
            {
                filteredLogs = filteredLogs.Where(l => l.Action == SelectedAction).ToList();
            }

            if (SelectedUser != null && !string.IsNullOrEmpty(SelectedUser.Id))
            {
                filteredLogs = filteredLogs.Where(l => l.PerformedBy == SelectedUser.Id).ToList();
            }

            if (TimestampFrom.HasValue)
            {
                filteredLogs = filteredLogs.Where(l => l.Timestamp >= TimestampFrom.Value).ToList();
            }

            if (TimestampTo.HasValue)
            {
                filteredLogs = filteredLogs.Where(l => l.Timestamp <= TimestampTo.Value).ToList();
            }

            _assetLogViewModel.AssetLogs = new ObservableCollection<AssetLog>(filteredLogs);
            _assetLogViewModel.NotifyAssetLogsChanged();
        }

        public void ClearFilters(bool onLogout = false)
        {
            AssetName = string.Empty;
            SelectedAction = Actions.FirstOrDefault();
            SelectedUser = Users.FirstOrDefault();
            TimestampFrom = null;
            TimestampTo = null;

            if (!onLogout)
            {
                ApplyFilter();
            }
        }

        public void UsersChanged(object sender, EventArgs e)
        {
            Users = new ObservableCollection<UserProfile> { new UserProfile { Id = null, DisplayName = "" } };
            LoadUsersAsync();
            OnPropertyChanged(nameof(Users));
        }

        public void SubscribeToManageUsersEvents(ManageUsersViewModel manageUsersViewModel)
        {
            manageUsersViewModel.UsersChanged += UsersChanged;
        }
    }
}
