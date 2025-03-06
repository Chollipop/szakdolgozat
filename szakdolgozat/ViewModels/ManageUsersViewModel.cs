using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using szakdolgozat.Services;

namespace szakdolgozat.ViewModels
{
    public class ManageUsersViewModel : BaseViewModel
    {
        private Brush _borderBrush = Brushes.Gray;
        private bool _isInputValid;
        private string _inviteEmailText;

        public ObservableCollection<User> Users { get; set; }
        public ObservableCollection<string> Roles { get; set; }

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

        public string InviteEmailText
        {
            get => _inviteEmailText;
            set
            {
                _inviteEmailText = value;
                if (IsValidEmail(value))
                {
                    IsInputValid = true;
                    BorderBrush = Brushes.Gray;
                }
                else
                {
                    IsInputValid = false;
                    BorderBrush = Brushes.Red;
                }
                OnPropertyChanged(nameof(InviteEmailText));
                OnPropertyChanged(nameof(IsInputValid));
                OnPropertyChanged(nameof(BorderBrush));
            }
        }

        public bool CanSaveChanges
        {
            get
            {
                return Users.Any(user => user.Role != user.CurrentRole);
            }
        }

        public ICommand SaveChangesCommand { get; }
        public ICommand InviteUserCommand { get; }
        public ICommand BulkInviteCommand { get; }

        public ManageUsersViewModel()
        {
            Users = new ObservableCollection<User>();
            Roles = new ObservableCollection<string> { "Admin", "Edit", "View" };
            OnPropertyChanged(nameof(Roles));

            _ = UpdateUsersAsync();

            Users.CollectionChanged += (sender, e) => OnPropertyChanged(nameof(CanSaveChanges));

            SaveChangesCommand = new RelayCommand(SaveChanges);
            InviteUserCommand = new RelayCommand(InviteUser);
            BulkInviteCommand = new RelayCommand(BulkInvite);
        }

        private async Task UpdateUsersAsync()
        {
            var userProfiles = await AuthenticationService.Instance.GetAllUsersAsync();
            Users.Clear();
            foreach (var userProfile in userProfiles)
            {
                if (userProfile.Email == AuthenticationService.Instance.CurrentUser.Username)
                {
                    continue;
                }

                var user = new User
                {
                    Id = userProfile.Id,
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
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            try
            {
                var emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                return Regex.IsMatch(email, emailPattern);
            }
            catch
            {
                return false;
            }
        }

        public void SaveChanges()
        {
            bool allSuccess = true;
            foreach (var user in Users)
            {
                if (user.Role != user.CurrentRole)
                {
                    bool isAssignmentSuccess = AuthenticationService.Instance.AssignAppRoleToUserAsync(user.Id, user.Role).Result;
                    bool isRevokeSuccess = AuthenticationService.Instance.RevokeSignInSessionsAsync(user.Id).Result;
                    if (isAssignmentSuccess && isRevokeSuccess)
                    {
                        user.CurrentRole = user.Role;
                    }
                    else
                    {
                        allSuccess = false;
                    }
                }
            }

            if (allSuccess)
            {
                MessageBox.Show("Roles updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Some roles could not be updated.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void InviteUser()
        {
            bool invitationSuccess = AuthenticationService.Instance.SendInvitationAsync(InviteEmailText).Result;

            if (invitationSuccess)
            {
                MessageBox.Show("Invitation sent successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Error sending invitation.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            _ = UpdateUsersAsync();
        }

        public void BulkInvite()
        {

        }
    }

    public class User
    {
        public event EventHandler RoleChanged;
        public event EventHandler CurrentRoleChanged;

        public string Id { get; set; }

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
