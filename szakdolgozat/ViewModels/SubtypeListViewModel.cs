using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using szakdolgozat.Models;
using szakdolgozat.Views;

namespace szakdolgozat.ViewModels
{
    public class SubtypeListViewModel : BaseViewModel
    {
        private Subtype _subtype;

        public event EventHandler SubtypesChanged;

        public ObservableCollection<Subtype> Subtypes { get; set; }

        public Subtype SelectedSubtype
        {
            get => _subtype;
            set
            {
                _subtype = value;
                OnPropertyChanged(nameof(SelectedSubtype));
            }
        }

        public ICommand AddSubtypeCommand { get; }
        public ICommand DeleteSubtypeCommand { get; }

        public SubtypeListViewModel()
        {
            LoadSubtypes();

            AddSubtypeCommand = new RelayCommand(AddSubtype);
            DeleteSubtypeCommand = new RelayCommand(DeleteSubtype, CanDeleteSubtype);

            App.ServiceProvider.GetRequiredService<AssetFilterViewModel>().SubscribeToSubtypeEvents(this);
        }

        private async Task LoadSubtypes()
        {
            using (var scope = App.ServiceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AssetDbContext>();
                var subtypesList = await context.Subtypes
                    .Include(s => s.AssetType)
                    .Select(s => new Subtype
                    {
                        TypeID = s.TypeID,
                        TypeName = s.TypeName,
                        AssetTypeID = s.AssetTypeID,
                        AssetType = s.AssetType
                    })
                    .ToListAsync();
                Subtypes = new ObservableCollection<Subtype>(subtypesList);
                OnPropertyChanged(nameof(Subtypes));

                SubtypesChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private async void AddSubtype()
        {
            var viewModel = new SubtypeDialogViewModel();
            var assSubtypeWindow = new SubtypeDialogView(viewModel);
            assSubtypeWindow.Title = "Add Subtype";
            if (assSubtypeWindow.ShowDialog() == true)
            {
                var newSubtype = viewModel.Subtype;
                using (var scope = App.ServiceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AssetDbContext>();
                    if (newSubtype.AssetType != null)
                    {
                        context.AssetTypes.Attach(newSubtype.AssetType);
                    }
                    context.Subtypes.Add(newSubtype);

                    await context.SaveChangesAsync();
                }
                Subtypes.Add(newSubtype);
                OnPropertyChanged(nameof(Subtypes));
                OnSubtypesChanged();
            }
        }

        private bool CanDeleteSubtype()
        {
            if (SelectedSubtype == null)
            {
                return false;
            }

            using (var scope = App.ServiceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AssetDbContext>();
                var asset = context.Assets.FirstOrDefault(a => a.SubtypeID == SelectedSubtype.TypeID);
                return asset == null;
            }
        }

        private async void DeleteSubtype()
        {
            if (SelectedSubtype != null)
            {
                var result = MessageBox.Show($"Are you sure you want to delete the subtype '{SelectedSubtype.TypeName}'?", "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    using (var scope = App.ServiceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<AssetDbContext>();

                        context.Subtypes.Remove(SelectedSubtype);

                        await context.SaveChangesAsync();
                    }

                    var deletedAsset = SelectedSubtype;
                    int index = Subtypes.IndexOf(SelectedSubtype);
                    Subtypes.RemoveAt(index);
                    OnPropertyChanged(nameof(Subtypes));
                    OnSubtypesChanged();
                }
            }
        }

        protected virtual void OnSubtypesChanged()
        {
            SubtypesChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
