using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using szakdolgozat.Models;
using szakdolgozat.Services;

namespace szakdolgozat.ViewModels
{
    public class AssetFilterViewModel : BaseViewModel
    {
        private AssetListViewModel AssetListViewModel;

        private string _assetName;
        private AssetType _selectedAssetType;
        private Subtype _selectedSubtype;
        private UserProfile _selectedOwner;
        private string _location;
        private DateTime? _purchaseDateFrom;
        private DateTime? _purchaseDateTo;
        private decimal? _valueFrom;
        private decimal? _valueTo;
        private string _selectedStatus;

        public string AssetName
        {
            get => _assetName;
            set
            {
                _assetName = value;
                OnPropertyChanged(nameof(AssetName));
            }
        }

        public ObservableCollection<AssetType> AssetTypes { get; } = new ObservableCollection<AssetType>();
        public AssetType SelectedAssetType
        {
            get => _selectedAssetType;
            set
            {
                _selectedAssetType = value;
                OnPropertyChanged(nameof(SelectedAssetType));
            }
        }

        public ObservableCollection<Subtype> Subtypes { get; } = new ObservableCollection<Subtype>();
        public Subtype SelectedSubtype
        {
            get => _selectedSubtype;
            set
            {
                _selectedSubtype = value;
                OnPropertyChanged(nameof(SelectedSubtype));
            }
        }

        public ObservableCollection<UserProfile> Owners { get; } = new ObservableCollection<UserProfile>();
        public UserProfile SelectedOwner
        {
            get => _selectedOwner;
            set
            {
                _selectedOwner = value;
                OnPropertyChanged(nameof(SelectedOwner));
            }
        }

        public string Location
        {
            get => _location;
            set
            {
                _location = value;
                OnPropertyChanged(nameof(Location));
            }
        }

        public DateTime? PurchaseDateFrom
        {
            get => _purchaseDateFrom;
            set
            {
                _purchaseDateFrom = value;
                OnPropertyChanged(nameof(PurchaseDateFrom));
            }
        }

        public DateTime? PurchaseDateTo
        {
            get => _purchaseDateTo;
            set
            {
                _purchaseDateTo = value;
                OnPropertyChanged(nameof(PurchaseDateTo));
            }
        }

        public decimal? ValueFrom
        {
            get => _valueFrom;
            set
            {
                _valueFrom = value;
                OnPropertyChanged(nameof(ValueFrom));
            }
        }

        public decimal? ValueTo
        {
            get => _valueTo;
            set
            {
                _valueTo = value;
                OnPropertyChanged(nameof(ValueTo));
            }
        }

        public ObservableCollection<string> Statuses { get; } = new ObservableCollection<string>();
        public string SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                _selectedStatus = value;
                OnPropertyChanged(nameof(SelectedStatus));
            }
        }

        public ICommand ApplyFilterCommand { get; }
        public ICommand ClearFilterCommand { get; }

        public AssetFilterViewModel()
        {
            AssetListViewModel = App.ServiceProvider.GetRequiredService<AssetListViewModel>();
            ApplyFilterCommand = new RelayCommand(ApplyFilter);
            ClearFilterCommand = new RelayCommand(ClearFilters);
            Owners = new ObservableCollection<UserProfile> { new UserProfile { Id = null, DisplayName = "" } };
            LoadOwnersAsync();
            AssetTypes = new ObservableCollection<AssetType> { new AssetType { TypeID = -1, TypeName = "" } };
            foreach (var assetType in GetAllAssetTypes())
            {
                AssetTypes.Add(assetType);
            }
            Subtypes = new ObservableCollection<Subtype> { new Subtype { TypeID = -1, TypeName = "" } };
            foreach (var subtype in GetAllSubtypes())
            {
                Subtypes.Add(subtype);
            }
            Statuses = new ObservableCollection<string> { "", "Active", "Retired" };
            SelectedStatus = Statuses[1];
            AssetListViewModel.AssetsChanged += OnAssetsChanged;
        }

        private async void LoadOwnersAsync()
        {
            var owners = await AuthenticationService.Instance.GetAllUsersAsync();
            foreach (var owner in owners)
            {
                Owners.Add(owner);
            }
        }

        private void OnAssetsChanged(object sender, EventArgs e)
        {
            ApplyFilter();
        }

        private IEnumerable<AssetType> GetAllAssetTypes()
        {
            using (var scope = App.ServiceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AssetDbContext>();
                return context.AssetTypes.ToList();
            }
        }

        private IEnumerable<Subtype> GetAllSubtypes()
        {
            using (var scope = App.ServiceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AssetDbContext>();
                return context.Subtypes.ToList();
            }
        }

        private void ApplyFilter()
        {
            List<Asset> filteredAssets;
            using (var scope = App.ServiceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AssetDbContext>();
                var assetsList = context.Assets
                    .Include(a => a.AssetType)
                    .Include(a => a.Subtype)
                    .Select(a => new Asset
                    {
                        AssetID = a.AssetID,
                        AssetName = a.AssetName,
                        AssetTypeID = a.AssetTypeID,
                        AssetType = a.AssetType,
                        SubtypeID = a.SubtypeID,
                        Subtype = a.Subtype,
                        Owner = a.Owner,
                        Location = a.Location,
                        PurchaseDate = a.PurchaseDate,
                        Value = a.Value,
                        Status = a.Status,
                        Description = a.Description
                    })
                    .ToList();
                filteredAssets = new List<Asset>(assetsList);
            }

            if (!string.IsNullOrEmpty(AssetName))
            {
                filteredAssets = filteredAssets.Where(a => a.AssetName.Contains(AssetName, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (SelectedAssetType != null && SelectedAssetType.TypeID != -1)
            {
                filteredAssets = filteredAssets.Where(a => a.AssetTypeID == SelectedAssetType.TypeID).ToList();
            }

            if (SelectedSubtype != null && SelectedSubtype.TypeID != -1)
            {
                filteredAssets = filteredAssets.Where(a => a.SubtypeID == SelectedSubtype.TypeID).ToList();
            }

            if (SelectedOwner != null && !string.IsNullOrEmpty(SelectedOwner.Id))
            {
                filteredAssets = filteredAssets.Where(a => a.Owner == SelectedOwner.Id).ToList();
            }

            if (!string.IsNullOrEmpty(Location))
            {
                filteredAssets = filteredAssets.Where(a => a.Location.Contains(Location, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (PurchaseDateFrom.HasValue)
            {
                filteredAssets = filteredAssets.Where(a => a.PurchaseDate >= PurchaseDateFrom.Value).ToList();
            }

            if (PurchaseDateTo.HasValue)
            {
                filteredAssets = filteredAssets.Where(a => a.PurchaseDate <= PurchaseDateTo.Value).ToList();
            }

            if (ValueFrom.HasValue)
            {
                filteredAssets = filteredAssets.Where(a => a.Value >= ValueFrom.Value).ToList();
            }

            if (ValueTo.HasValue)
            {
                filteredAssets = filteredAssets.Where(a => a.Value <= ValueTo.Value).ToList();
            }

            if (!string.IsNullOrEmpty(SelectedStatus))
            {
                filteredAssets = filteredAssets.Where(a => a.Status == SelectedStatus).ToList();
            }

            AssetListViewModel.Assets = new ObservableCollection<Asset>(filteredAssets);
            AssetListViewModel.NotifyAssetsChanged();
        }

        private void ClearFilters()
        {
            AssetName = string.Empty;
            SelectedAssetType = AssetTypes.FirstOrDefault();
            SelectedSubtype = Subtypes.FirstOrDefault();
            SelectedOwner = Owners.FirstOrDefault();
            Location = string.Empty;
            PurchaseDateFrom = null;
            PurchaseDateTo = null;
            ValueFrom = null;
            ValueTo = null;
            SelectedStatus = Statuses[1];

            ApplyFilter();
        }
    }
}
