using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using szakdolgozat.Models;

namespace szakdolgozat.ViewModels
{
    public class SubtypeDialogViewModel : BaseViewModel
    {
        private Subtype _subtype;

        private Brush _borderBrush = Brushes.Gray;
        private bool _isInputValid = true;

        public ObservableCollection<AssetType> AssetTypes { get; set; }

        public Subtype Subtype
        {
            get => _subtype;
            set
            {
                _subtype = value;
                OnPropertyChanged(nameof(Subtype));
            }
        }

        public string SubtypeName
        {
            get => _subtype.TypeName;
            set
            {
                _subtype.TypeName = value;
                OnPropertyChanged(nameof(SubtypeName));
            }
        }

        public AssetType AssetType
        {
            get => _subtype.AssetType;
            set
            {
                _subtype.AssetType = value;
                OnPropertyChanged(nameof(AssetType));
            }
        }

        public AssetType SelectedAssetType
        {
            get => AssetTypes.FirstOrDefault(at => at == _subtype.AssetType);
            set
            {
                _subtype.AssetType = value;
                OnPropertyChanged(nameof(SelectedAssetType));
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

        public ICommand SubmitCommand { get; }
        public ICommand CancelCommand { get; }

        public SubtypeDialogViewModel()
        {
            _subtype = new Subtype();
            AssetTypes = new ObservableCollection<AssetType>(GetAllAssetTypes());

            if (AssetTypes.Any())
            {
                SelectedAssetType = AssetTypes.First();
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
