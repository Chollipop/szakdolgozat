using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using szakdolgozat.Models;
using szakdolgozat.Services;

namespace szakdolgozat.ViewModels
{
    public class AssetAssignmentFilterViewModel : BaseViewModel
    {
        private AssetAssignmentListViewModel _assetAssignmentListViewModel;

        private string _assetName;
        private UserProfile _selectedUser;
        private DateTime? _assignmentDateFrom;
        private DateTime? _assignmentDateTo;
        private DateTime? _returnDateFrom;
        private DateTime? _returnDateTo;

        public string AssetName
        {
            get => _assetName;
            set
            {
                _assetName = value;
                OnPropertyChanged(nameof(AssetName));
            }
        }

        public ObservableCollection<UserProfile> Users { get; } = new ObservableCollection<UserProfile>();
        public UserProfile SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;
                OnPropertyChanged(nameof(SelectedUser));
            }
        }

        public DateTime? AssignmentDateFrom
        {
            get => _assignmentDateFrom;
            set
            {
                _assignmentDateFrom = value;
                OnPropertyChanged(nameof(AssignmentDateFrom));
            }
        }

        public DateTime? AssignmentDateTo
        {
            get => _assignmentDateTo;
            set
            {
                _assignmentDateTo = value;
                OnPropertyChanged(nameof(AssignmentDateTo));
            }
        }

        public DateTime? ReturnDateFrom
        {
            get => _returnDateFrom;
            set
            {
                _returnDateFrom = value;
                OnPropertyChanged(nameof(ReturnDateFrom));
            }
        }

        public DateTime? ReturnDateTo
        {
            get => _returnDateTo;
            set
            {
                _returnDateTo = value;
                OnPropertyChanged(nameof(ReturnDateTo));
            }
        }

        public ICommand ApplyFilterCommand { get; }
        public ICommand ClearFilterCommand { get; }

        public AssetAssignmentFilterViewModel()
        {
            _assetAssignmentListViewModel = App.ServiceProvider.GetRequiredService<AssetAssignmentListViewModel>();
            ApplyFilterCommand = new RelayCommand(ApplyFilter);
            ClearFilterCommand = new RelayCommand(ClearFilters);

            Users = new ObservableCollection<UserProfile> { new UserProfile { Id = null, DisplayName = "" } };
            LoadUsersAsync();
            AssignmentDateFrom = DateTime.Today;
            _assetAssignmentListViewModel.AssetAssignmentsChanged += OnAssetAssignmentsChanged;
        }

        private async void LoadUsersAsync()
        {
            var users = await AuthenticationService.Instance.GetAllUsersAsync();
            foreach (var user in users)
            {
                Users.Add(user);
            }
        }

        private void OnAssetAssignmentsChanged(object sender, EventArgs e)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            List<AssetAssignment> filteredAssignments;
            using (var scope = App.ServiceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AssetDbContext>();
                var assignmentsList = context.AssetAssignments
                    .Include(a => a.Asset)
                    .Select(a => new AssetAssignment
                    {
                        AssignmentID = a.AssignmentID,
                        AssetID = a.AssetID,
                        Asset = a.Asset,
                        User = a.User,
                        AssignmentDate = a.AssignmentDate,
                        ReturnDate = a.ReturnDate
                    })
                    .ToList();
                filteredAssignments = new List<AssetAssignment>(assignmentsList);
            }

            if (!string.IsNullOrEmpty(AssetName))
            {
                filteredAssignments = filteredAssignments.Where(a => a.Asset.AssetName.Contains(AssetName, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (SelectedUser != null && !string.IsNullOrEmpty(SelectedUser.Id))
            {
                filteredAssignments = filteredAssignments.Where(a => a.User == SelectedUser.Id).ToList();
            }
            if (AssignmentDateFrom.HasValue)
            {
                filteredAssignments = filteredAssignments.Where(a => a.AssignmentDate >= AssignmentDateFrom.Value).ToList();
            }

            if (AssignmentDateTo.HasValue)
            {
                filteredAssignments = filteredAssignments.Where(a => a.AssignmentDate <= AssignmentDateTo.Value).ToList();
            }

            if (ReturnDateFrom.HasValue)
            {
                filteredAssignments = filteredAssignments.Where(a => a.ReturnDate >= ReturnDateFrom.Value).ToList();
            }

            if (ReturnDateTo.HasValue)
            {
                filteredAssignments = filteredAssignments.Where(a => a.ReturnDate <= ReturnDateTo.Value).ToList();
            }

            _assetAssignmentListViewModel.AssetAssignments = new ObservableCollection<AssetAssignment>(filteredAssignments);
            _assetAssignmentListViewModel.NotifyAssetAssignmentsChanged();
        }


        private void ClearFilters()
        {
            AssetName = string.Empty;
            SelectedUser = Users.FirstOrDefault();
            AssignmentDateFrom = DateTime.Today;
            AssignmentDateTo = null;
            ReturnDateFrom = null;
            ReturnDateTo = null;

            ApplyFilter();
        }
    }
}
