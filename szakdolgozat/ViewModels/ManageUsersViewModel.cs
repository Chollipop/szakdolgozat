using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using szakdolgozat.Services;

namespace szakdolgozat.ViewModels
{
    public class ManageUsersViewModel : BaseViewModel
    {
        public ObservableCollection<User> Users { get; set; }
        public ObservableCollection<string> Roles { get; set; }

        public bool CanSaveChanges
        {
            get
            {
                return Users.Any(user => user.Role != user.CurrentRole);
            }
        }

        public ICommand SaveChangesCommand { get; }

        public ManageUsersViewModel()
        {
            Users = new ObservableCollection<User>();
            Roles = new ObservableCollection<string> { "Admin", "Edit", "View" };
            OnPropertyChanged(nameof(Roles));

            var userProfiles = AuthenticationService.Instance.GetAllUsersAsync().Result;
            foreach (var userProfile in userProfiles)
            {
                if (userProfile.Email == AuthenticationService.Instance.CurrentUser.Username)
                {
                    continue;
                }

                var user = new User
                {
                    Name = userProfile.DisplayName,
                    Email = userProfile.Email,
                    Role = userProfile.GetRole(),
                    CurrentRole = userProfile.GetRole()
                };
                Users.Add(user);

                user.RoleChanged += (sender, e) => OnPropertyChanged(nameof(CanSaveChanges));
                user.CurrentRoleChanged += (sender, e) => OnPropertyChanged(nameof(CanSaveChanges));
            }
            OnPropertyChanged(nameof(Users));

            Users.CollectionChanged += (sender, e) => OnPropertyChanged(nameof(CanSaveChanges));

            SaveChangesCommand = new RelayCommand(SaveChanges);
        }

        public void SaveChanges()
        {
            //TODO
        }
    }

    public class User
    {
        public event EventHandler RoleChanged;
        public event EventHandler CurrentRoleChanged;

        public string Name { get; set; }
        public string Email { get; set; }

        private string _role;
        public string Role
        {
            get => _role;
            set
            {
                if (_role != value)
                {
                    _role = value;
                    RoleChanged?.Invoke(this, new System.EventArgs());
                }
            }
        }

        private string _currentRole;
        public string CurrentRole
        {
            get => _currentRole;
            set
            {
                if (_currentRole != value)
                {
                    _currentRole = value;
                    CurrentRoleChanged?.Invoke(this, new System.EventArgs());
                }
            }
        }
    }
}
