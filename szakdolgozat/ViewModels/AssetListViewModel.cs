using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using szakdolgozat.Models;
using szakdolgozat.Services;
using szakdolgozat.Views;

namespace szakdolgozat.ViewModels
{
    public class AssetListViewModel : BaseViewModel
    {
        private Asset _selectedAsset;

        public ObservableCollection<Asset> Assets { get; set; }

        public Asset SelectedAsset
        {
            get => _selectedAsset;
            set
            {
                _selectedAsset = value;
                OnPropertyChanged(nameof(SelectedAsset));
            }
        }

        public event EventHandler AssetsChanged;
        public event EventHandler AssetLogsChanged;

        public ICommand AddAssetCommand { get; }
        public ICommand DeleteAssetCommand { get; }
        public ICommand UpdateAssetCommand { get; }

        public AssetListViewModel()
        {
            LoadAssets();

            AddAssetCommand = new RelayCommand(AddAsset);
            DeleteAssetCommand = new RelayCommand(DeleteAsset, CanDeleteAsset);
            UpdateAssetCommand = new RelayCommand(UpdateAsset, CanUpdateAsset);
        }

        private async Task LoadAssets()
        {
            using (var scope = App.ServiceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AssetDbContext>();
                var assetsList = await context.Assets
                    .Include(a => a.AssetType)
                    .Include(a => a.Subtype)
                    .Where(a => a.Status == "Active")
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
                    .ToListAsync();
                Assets = new ObservableCollection<Asset>(assetsList);
                OnPropertyChanged(nameof(Assets));

                AssetsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private async void AddAsset()
        {
            var viewModel = new AssetDialogViewModel();
            var addAssetWindow = new AssetDialogView(viewModel);
            addAssetWindow.Title = "Add Asset";
            if (addAssetWindow.ShowDialog() == true)
            {
                var newAsset = viewModel.Asset;
                using (var scope = App.ServiceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AssetDbContext>();
                    if (newAsset.AssetType != null)
                    {
                        context.AssetTypes.Attach(newAsset.AssetType);
                    }

                    if (newAsset.Subtype?.TypeID == -1)
                    {

                        newAsset.SubtypeID = null;
                        newAsset.Subtype = null;
                    }
                    else
                    {
                        context.Subtypes.Attach(newAsset.Subtype);
                    }

                    context.Assets.Add(newAsset);

                    await context.SaveChangesAsync();

                    var log = new AssetLog
                    {
                        AssetID = newAsset.AssetID,
                        Action = "Added",
                        Timestamp = DateTime.Now,
                        PerformedBy = AuthenticationService.Instance.CurrentUser.GetTenantProfiles().ElementAt(0).Oid
                    };
                    context.AssetLogs.Add(log);

                    await context.SaveChangesAsync();
                }
                Assets.Add(newAsset);
                OnPropertyChanged(nameof(Assets));
                OnAssetsChanged();
                OnAssetLogsChanged();
            }
        }

        private async void UpdateAsset()
        {
            if (SelectedAsset != null)
            {
                var viewModel = new AssetDialogViewModel(SelectedAsset);
                var addAssetWindow = new AssetDialogView(viewModel);
                addAssetWindow.Title = "Update Asset";
                if (addAssetWindow.ShowDialog() == true)
                {
                    var updatedAsset = viewModel.Asset;
                    using (var scope = App.ServiceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<AssetDbContext>();

                        SelectedAsset.AssetName = updatedAsset.AssetName;
                        SelectedAsset.AssetTypeID = updatedAsset.AssetTypeID;
                        SelectedAsset.AssetType = updatedAsset.AssetType;
                        if (updatedAsset.SubtypeID == -1)
                        {
                            SelectedAsset.SubtypeID = null;
                            SelectedAsset.Subtype = null;
                        }
                        else
                        {
                            SelectedAsset.SubtypeID = updatedAsset.SubtypeID;
                            SelectedAsset.Subtype = updatedAsset.Subtype;
                        }
                        SelectedAsset.Owner = updatedAsset.Owner;
                        SelectedAsset.Location = updatedAsset.Location;
                        SelectedAsset.PurchaseDate = updatedAsset.PurchaseDate;
                        SelectedAsset.Value = updatedAsset.Value;
                        SelectedAsset.Status = updatedAsset.Status;
                        SelectedAsset.Description = updatedAsset.Description;

                        context.Assets.Update(SelectedAsset);

                        var log = new AssetLog
                        {
                            AssetID = SelectedAsset.AssetID,
                            Action = "Updated",
                            Timestamp = DateTime.Now,
                            PerformedBy = AuthenticationService.Instance.CurrentUser.GetTenantProfiles().ElementAt(0).Oid
                        };
                        context.AssetLogs.Add(log);

                        await context.SaveChangesAsync();
                    }
                    int index = Assets.IndexOf(SelectedAsset);
                    Assets.RemoveAt(index);
                    Assets.Insert(index, updatedAsset);
                    OnPropertyChanged(nameof(Assets));
                    OnAssetsChanged();
                    OnAssetLogsChanged();
                }
            }
        }

        private bool CanUpdateAsset()
        {
            return SelectedAsset != null;
        }

        private async void DeleteAsset()
        {
            if (SelectedAsset != null && SelectedAsset.Status != "Retired")
            {
                var result = MessageBox.Show($"Are you sure you want to delete the asset '{SelectedAsset.AssetName}'?", "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    using (var scope = App.ServiceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<AssetDbContext>();
                        SelectedAsset.Status = "Retired";
                        context.Assets.Update(SelectedAsset);

                        var log = new AssetLog
                        {
                            AssetID = SelectedAsset.AssetID,
                            Action = "Deleted",
                            Timestamp = DateTime.Now,
                            PerformedBy = AuthenticationService.Instance.CurrentUser.GetTenantProfiles().ElementAt(0).Oid
                        };
                        context.AssetLogs.Add(log);

                        await context.SaveChangesAsync();
                    }
                    var deletedAsset = SelectedAsset;
                    int index = Assets.IndexOf(SelectedAsset);
                    Assets.RemoveAt(index);
                    Assets.Insert(index, deletedAsset);
                    OnPropertyChanged(nameof(Assets));
                    OnAssetsChanged();
                    OnAssetLogsChanged();
                }
            }
        }

        private bool CanDeleteAsset()
        {
            try
            {
                if (SelectedAsset == null)
                {
                    return false;
                }

                using (var scope = App.ServiceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AssetDbContext>();
                    if (context.AssetAssignments == null)
                    {
                        return false;
                    }

                    bool hasAssignments = context.AssetAssignments
                        .Any(aa => aa.AssetID == SelectedAsset.AssetID && DateTime.Now <= aa.ReturnDate);

                    return SelectedAsset.Status != "Retired" && !hasAssignments;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        protected virtual void OnAssetsChanged()
        {
            AssetsChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnAssetLogsChanged()
        {
            AssetLogsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void NotifyAssetsChanged()
        {
            OnPropertyChanged(nameof(Assets));
        }
    }
}
