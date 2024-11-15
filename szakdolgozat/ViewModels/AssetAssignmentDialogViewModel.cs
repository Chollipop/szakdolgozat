using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using szakdolgozat.Models;
using szakdolgozat.Services;

namespace szakdolgozat.ViewModels
{
    public class AssetAssignmentDialogViewModel : BaseViewModel
    {
        private AssetAssignment _assetAssignment;

        public ObservableCollection<UserProfile> Users { get; set; }
        public ObservableCollection<Asset> Assets { get; set; }

        public AssetAssignment AssetAssignment
        {
            get => _assetAssignment;
            set
            {
                _assetAssignment = value;
                OnPropertyChanged(nameof(Asset));
            }
        }

        public Asset Asset
        {
            get => _assetAssignment.Asset;
            set
            {
                _assetAssignment.Asset = value;
                OnPropertyChanged(nameof(Asset));
            }
        }

        public string User
        {
            get => _assetAssignment.GetUserFullName();
            set
            {
                _assetAssignment.User = value;
                OnPropertyChanged(nameof(User));
            }
        }

        public DateTime? AssignmentDate
        {
            get => _assetAssignment.AssignmentDate;
            set
            {
                _assetAssignment.AssignmentDate = value;
                OnPropertyChanged(nameof(AssignmentDate));
            }
        }

        public DateTime? ReturnDate
        {
            get => _assetAssignment.ReturnDate;
            set
            {
                _assetAssignment.ReturnDate = value;
                OnPropertyChanged(nameof(ReturnDate));
            }
        }

        public UserProfile SelectedUser
        {
            get => Users.FirstOrDefault(u => u.Id == _assetAssignment.User);
            set
            {
                _assetAssignment.User = value?.Id;
                OnPropertyChanged(nameof(SelectedUser));
            }
        }

        public Asset SelectedAsset
        {
            get => Assets.FirstOrDefault(at => at == _assetAssignment.Asset);
            set
            {
                _assetAssignment.Asset = value;
                OnPropertyChanged(nameof(SelectedAsset));
            }
        }

        public ICommand SubmitCommand { get; }
        public ICommand CancelCommand { get; }

        public AssetAssignmentDialogViewModel(AssetAssignment assetAssignment = null)
        {
            _assetAssignment = assetAssignment ?? new AssetAssignment();
            Users = new ObservableCollection<UserProfile>(AuthenticationService.Instance.GetAllUsers());
            Assets = new ObservableCollection<Asset>(GetAllAssets());

            if (_assetAssignment.User != null)
            {
                SelectedUser = Users.Where(u => u.Id == _assetAssignment.User).First();
            }
            else if (Users.Any())
            {
                SelectedUser = Users.First();
            }

            if (_assetAssignment.Asset != null)
            {
                SelectedAsset = Assets.Where(at => at.AssetID == _assetAssignment.AssetID).First();
            }
            else if (Assets.Any())
            {
                SelectedAsset = Assets.First();
            }

            if (_assetAssignment.AssignmentDate != null)
            {
                AssignmentDate = _assetAssignment.AssignmentDate;
            }
            else
            {
                AssignmentDate = DateTime.Now;
            }

            if (_assetAssignment.ReturnDate != null)
            {
                ReturnDate = _assetAssignment.ReturnDate;
            }
            else
            {
                ReturnDate = DateTime.Now;
            }

            SubmitCommand = new RelayCommand(Submit);
            CancelCommand = new RelayCommand(Cancel);
        }

        private void Submit()
        {
            var window = Application.Current.Windows.OfType<Window>().SingleOrDefault(w => w.IsActive);
            if (window != null)
            {
                window.DialogResult = true;
                window.Close();
            }
        }

        private void Cancel()
        {
            var window = Application.Current.Windows.OfType<Window>().SingleOrDefault(w => w.IsActive);
            if (window != null)
            {
                window.DialogResult = false;
                window.Close();
            }
        }

        private IEnumerable<Asset> GetAllAssets()
        {
            using (var scope = App.ServiceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AssetDbContext>();
                var assignedAssetIds = context.AssetAssignments.Select(aa => aa.AssetID).ToList();
                return context.Assets
                    .Where(a => a.Status == "Active" && !assignedAssetIds.Contains(a.AssetID))
                    .ToList();
            }
        }
    }
}
