using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using szakdolgozat.Models;
using szakdolgozat.Services;

namespace szakdolgozat.ViewModels
{
    public class AssetDialogViewModel : BaseViewModel
    {
        private Asset _asset;

        private Brush _borderBrush = Brushes.Gray;
        private bool _isInputValid;
        private string _valueText;
        private bool _hasAssignment;

        public ObservableCollection<UserProfile> Users { get; set; }
        public ObservableCollection<AssetType> AssetTypes { get; set; }
        public ObservableCollection<string> StatusOptions { get; set; }

        public Asset Asset
        {
            get => _asset;
            set
            {
                _asset = value;
                OnPropertyChanged(nameof(Asset));
            }
        }

        public string AssetName
        {
            get => _asset.AssetName;
            set
            {
                _asset.AssetName = value;
                OnPropertyChanged(nameof(AssetName));
            }
        }

        public AssetType AssetType
        {
            get => _asset.AssetType;
            set
            {
                _asset.AssetType = value;
                OnPropertyChanged(nameof(AssetType));
            }
        }

        public string Owner
        {
            get => _asset.GetOwnerFullName();
            set
            {
                _asset.Owner = value;
                OnPropertyChanged(nameof(Owner));
            }
        }

        public string Location
        {
            get => _asset.Location;
            set
            {
                _asset.Location = value;
                OnPropertyChanged(nameof(Location));
            }
        }

        public DateTime? PurchaseDate
        {
            get => _asset.PurchaseDate;
            set
            {
                _asset.PurchaseDate = value;
                OnPropertyChanged(nameof(PurchaseDate));
            }
        }

        public Brush BorderBrush
        {
            get => _borderBrush;
            private set
            {
                if (_borderBrush != value)
                {
                    _borderBrush = value;
                    OnPropertyChanged(nameof(BorderBrush));
                }
            }
        }

        public bool IsInputValid
        {
            get => _isInputValid;
            set
            {
                _isInputValid = value;
                OnPropertyChanged(nameof(IsInputValid));
            }
        }

        public string ValueText
        {
            get => _valueText;
            set
            {
                _valueText = value;
                if (decimal.TryParse(value, out decimal parsedValue) && parsedValue % 1 == 0)
                {
                    IsInputValid = true;
                    _asset.Value = parsedValue;
                    BorderBrush = Brushes.Gray;
                }
                else
                {
                    IsInputValid = false;
                    _asset.Value = null;
                    BorderBrush = Brushes.Red;
                }
                OnPropertyChanged(nameof(ValueText));
                OnPropertyChanged(nameof(Value));
                OnPropertyChanged(nameof(IsInputValid));
                OnPropertyChanged(nameof(BorderBrush));
            }
        }

        public decimal? Value
        {
            get => _asset.Value;
        }

        public string Status
        {
            get => _asset.Status;
            set
            {
                _asset.Status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public string Description
        {
            get => _asset.Description;
            set
            {
                _asset.Description = value;
                OnPropertyChanged(nameof(Description));
            }
        }

        public bool HasAssignment
        {
            get => _hasAssignment;
            private set
            {
                _hasAssignment = value;
                OnPropertyChanged(nameof(HasAssignment));
            }
        }

        public UserProfile SelectedUser
        {
            get => Users.FirstOrDefault(u => u.Id == _asset.Owner);
            set
            {
                _asset.Owner = value?.Id;
                OnPropertyChanged(nameof(SelectedUser));
            }
        }

        public AssetType SelectedAssetType
        {
            get => AssetTypes.FirstOrDefault(at => at == _asset.AssetType);
            set
            {
                _asset.AssetType = value;
                OnPropertyChanged(nameof(SelectedAssetType));
            }
        }

        public ICommand SubmitCommand { get; }
        public ICommand CancelCommand { get; }

        public AssetDialogViewModel(Asset asset = null)
        {
            _asset = asset ?? new Asset();
            Users = new ObservableCollection<UserProfile>(AuthenticationService.Instance.GetAllUsersAsync().Result);
            AssetTypes = new ObservableCollection<AssetType>(GetAllAssetTypes());
            StatusOptions = new ObservableCollection<string> { "Active", "Retired" };

            if (_asset.Owner != null)
            {
                SelectedUser = Users.Where(u => u.Id == _asset.Owner).First();
            }
            else if (Users.Any())
            {
                SelectedUser = Users.First();
            }

            if (_asset.AssetType != null)
            {
                SelectedAssetType = AssetTypes.Where(at => at.TypeID == _asset.AssetTypeID).First();
            }
            else if (AssetTypes.Any())
            {
                SelectedAssetType = AssetTypes.First();
            }

            if (_asset.Status != null)
            {
                Status = StatusOptions.Where(s => s == _asset.Status).First();
            }
            else if (StatusOptions.Any())
            {
                Status = StatusOptions.First();
            }

            if (_asset.PurchaseDate != null)
            {
                PurchaseDate = _asset.PurchaseDate;
            }
            else
            {
                PurchaseDate = DateTime.Now;
            }

            if (_asset.Value != null)
            {
                ValueText = _asset.Value.ToString();
            }

            SubmitCommand = new RelayCommand(Submit);
            CancelCommand = new RelayCommand(Cancel);

            CheckIfAssetHasAssignment();
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

        private void CheckIfAssetHasAssignment()
        {
            using (var scope = App.ServiceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AssetDbContext>();
                HasAssignment = !context.AssetAssignments
                    .Any(aa => aa.AssetID == Asset.AssetID && DateTime.Now <= aa.ReturnDate);
            }
        }

        private IEnumerable<AssetType> GetAllAssetTypes()
        {
            using (var scope = App.ServiceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AssetDbContext>();
                return context.AssetTypes.ToList();
            }
        }
    }
}
