using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Windows.Input;
using szakdolgozat.Models;
using szakdolgozat.Views;

namespace szakdolgozat.ViewModels
{
    public class AssetAssignmentListViewModel : BaseViewModel
    {
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

        private async Task LoadAssetAssignments()
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
            }
        }

        private async void AddAssetAssignment()
        {
            var viewModel = new AssetAssignmentDialogViewModel();
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
                var viewModel = new AssetAssignmentDialogViewModel(SelectedAssignment);
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
