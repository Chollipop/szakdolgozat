using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using szakdolgozat.Models;
using szakdolgozat.Views;

namespace szakdolgozat.ViewModels
{
    public class AssetAssignmentListViewModel : BaseViewModel
    {
        private ObservableCollection<Asset> _assignableAssets;
        private AssetAssignment _selectedAssignment;

        public ObservableCollection<AssetAssignment> AssetAssignments { get; set; }

        public AssetAssignment SelectedAssignment
        {
            get => _selectedAssignment;
            set
            {
                _selectedAssignment = value;
                OnPropertyChanged(nameof(SelectedAssignment));
            }
        }

        public event EventHandler AssetAssignmentsChanged;

        public ICommand AddAssetAssignmentCommand { get; }
        public ICommand UpdateAssetAssignmentCommand { get; }

        public AssetAssignmentListViewModel()
        {
            LoadAssetAssignments();

            AddAssetAssignmentCommand = new RelayCommand(AddAssetAssignment);
            UpdateAssetAssignmentCommand = new RelayCommand(UpdateAssetAssignment, CanUpdateAssetAssignment);
        }

        private void GetAllAssignableAssets()
        {
            using (var scope = App.ServiceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AssetDbContext>();
                var assetsList = context.Assets
                    .Where(a => a.Status == "Active" && !context.AssetAssignments
                    .Any(aa => aa.AssetID == a.AssetID && DateTime.Now <= aa.ReturnDate))
                    .Select(a => new Asset
                    {
                        AssetID = a.AssetID,
                        AssetName = a.AssetName,
                        AssetTypeID = a.AssetTypeID,
                        AssetType = a.AssetType,
                        Owner = a.Owner,
                        Location = a.Location,
                        PurchaseDate = a.PurchaseDate,
                        Value = a.Value,
                        Status = a.Status,
                        Description = a.Description
                    })
                    .ToList();
                _assignableAssets = new ObservableCollection<Asset>(assetsList);
            }
        }

        public async Task LoadAssetAssignments()
        {
            using (var scope = App.ServiceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AssetDbContext>();
                var assignmentsList = await context.AssetAssignments
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
                    .ToListAsync();
                AssetAssignments = new ObservableCollection<AssetAssignment>(assignmentsList);
                OnPropertyChanged(nameof(AssetAssignments));

                AssetAssignmentsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private async void AddAssetAssignment()
        {
            GetAllAssignableAssets();
            if (_assignableAssets.Count == 0)
            {
                MessageBox.Show("There are no assets available for assignment.", "No assets available", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var viewModel = new AssetAssignmentDialogViewModel(_assignableAssets);
            var addAssetWindow = new AssetAssignmentDialogView(viewModel);
            addAssetWindow.Title = "Add Assignment";
            if (addAssetWindow.ShowDialog() == true)
            {
                var newAssignment = viewModel.AssetAssignment;
                using (var scope = App.ServiceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AssetDbContext>();
                    if (newAssignment.Asset != null)
                    {
                        context.Assets.Attach(newAssignment.Asset);
                    }
                    context.AssetAssignments.Add(newAssignment);
                    await context.SaveChangesAsync();
                }
                AssetAssignments.Add(newAssignment);
                OnPropertyChanged(nameof(AssetAssignments));
                OnAssetAssignmentsChanged();
            }
        }

        private async void UpdateAssetAssignment()
        {
            if (SelectedAssignment != null)
            {
                GetAllAssignableAssets();
                var viewModel = new AssetAssignmentDialogViewModel(_assignableAssets, SelectedAssignment);
                var addAssetWindow = new AssetAssignmentDialogView(viewModel);
                addAssetWindow.Title = "Update Assignment";
                if (addAssetWindow.ShowDialog() == true)
                {
                    var updatedAssetAssignment = viewModel.AssetAssignment;
                    using (var scope = App.ServiceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<AssetDbContext>();

                        SelectedAssignment.Asset = updatedAssetAssignment.Asset;
                        SelectedAssignment.User = updatedAssetAssignment.User;
                        SelectedAssignment.AssignmentDate = updatedAssetAssignment.AssignmentDate;
                        SelectedAssignment.ReturnDate = updatedAssetAssignment.ReturnDate;

                        context.AssetAssignments.Update(SelectedAssignment);
                        await context.SaveChangesAsync();
                    }
                    int index = AssetAssignments.IndexOf(SelectedAssignment);
                    AssetAssignments.RemoveAt(index);
                    AssetAssignments.Insert(index, updatedAssetAssignment);
                    OnPropertyChanged(nameof(AssetAssignments));
                    OnAssetAssignmentsChanged();
                }
            }
        }

        private bool CanUpdateAssetAssignment()
        {
            return SelectedAssignment != null;
        }

        protected virtual void OnAssetAssignmentsChanged()
        {
            AssetAssignmentsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void NotifyAssetAssignmentsChanged()
        {
            OnPropertyChanged(nameof(AssetAssignments));
        }
    }
}
