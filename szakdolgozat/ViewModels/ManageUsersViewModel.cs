using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
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
        private User _selectedUser;

        public ObservableCollection<User> Users { get; set; }
        public ObservableCollection<string> Roles { get; set; }

        public User SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;
                OnPropertyChanged(nameof(SelectedUser));
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
        public ICommand DeleteUserCommand { get; }
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
            DeleteUserCommand = new RelayCommand(DeleteUserAsync, CanDeleteUser);
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

        private bool CanDeleteUser()
        {
            if (SelectedUser == null) return false;

            using (var scope = App.ServiceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AssetDbContext>();
                bool hasAsset = context.Assets.Any(a => a.Owner == SelectedUser.Id);
                bool hasAssetAssignment = context.AssetAssignments.Any(a => a.User == SelectedUser.Id);
                if (!hasAsset && !hasAssetAssignment)
                {
                    return true;
                }
            }
            return false;
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

        public void DeleteUserAsync()
        {
            if (SelectedUser != null)
            {
                var result = MessageBox.Show($"Are you sure you want to delete the user '{SelectedUser.Name}'?", "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    string unifiedRoleAssignmentId = AuthenticationService.Instance.GetRoleAssignmentIdByPrincipalIdAsync(SelectedUser.Id).Result;
                    AuthenticationService.Instance.DeleteRoleAssignmentAsync(unifiedRoleAssignmentId).Wait();
                    AuthenticationService.Instance.DeleteUserAsync(SelectedUser.Id).Wait();
                    _ = UpdateUsersAsync();
                }
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
            string csvFilePath = OpenFileDialogToSelectCsvFile();

            if (string.IsNullOrEmpty(csvFilePath))
            {
                MessageBox.Show("No file selected. Please select a valid CSV file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!File.Exists(csvFilePath))
            {
                MessageBox.Show("File not found. Please check the file path.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var emailList = ReadEmailsFromCsv(csvFilePath);

            if (emailList.Count == 0)
            {
                MessageBox.Show("No email addresses found in the CSV file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            foreach (var email in emailList)
            {
                if (!IsValidEmail(email))
                {
                    MessageBox.Show($"Invalid email format: {email}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    continue;
                }

                bool invitationSuccess = AuthenticationService.Instance.SendInvitationAsync(email).Result;
                if (invitationSuccess)
                {
                    MessageBox.Show($"Invitation sent to: {email}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Failed to send invitation to {email}.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            MessageBox.Show("Bulk invitation process completed.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            _ = UpdateUsersAsync();
        }

        private List<string> ReadEmailsFromCsv(string csvFilePath)
        {
            var emailList = new List<string>();

            try
            {
                using (var reader = new StreamReader(csvFilePath))
                {
                    var firstLine = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(firstLine))
                    {
                        MessageBox.Show("CSV file is empty.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return emailList;
                    }

                    bool isHeader = !IsValidEmail(firstLine.Split(',')[0]);

                    if (!isHeader)
                    {
                        emailList.Add(firstLine.Trim());
                    }

                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();

                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        var columns = line.Split(',');

                        if (columns.Length == 1 && IsValidEmail(columns[0]))
                        {
                            emailList.Add(columns[0].Trim());
                        }
                        else
                        {
                            MessageBox.Show($"Invalid email format found in CSV: {line}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Error reading the CSV file. Please ensure it is correctly formatted.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return emailList;
        }

        private string OpenFileDialogToSelectCsvFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                Title = "Select CSV File with Email Addresses"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                return openFileDialog.FileName;
            }

            return string.Empty;
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
