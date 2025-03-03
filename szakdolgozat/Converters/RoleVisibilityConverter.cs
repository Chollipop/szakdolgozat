using System.Globalization;
using System.Windows;
using System.Windows.Data;
using szakdolgozat.Services;

namespace szakdolgozat.Converters
{
    public class RoleVisibilityConverter : IValueConverter
    {
        private static readonly Dictionary<string, int> RoleHierarchy = new Dictionary<string, int>
        {
            { "view", 1 },
            { "edit", 2 },
            { "admin", 3 }
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string requiredRole && RoleHierarchy.ContainsKey(requiredRole))
            {
                var userRoles = AuthenticationService.Instance.GetUserRolesAsync().Result;
                foreach (var role in userRoles)
                {
                    if (RoleHierarchy.TryGetValue(role, out int userRoleLevel) && userRoleLevel >= RoleHierarchy[requiredRole])
                    {
                        return Visibility.Visible;
                    }
                }
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
