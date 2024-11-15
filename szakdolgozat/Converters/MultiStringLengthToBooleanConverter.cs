using System.Globalization;
using System.Windows.Data;

namespace szakdolgozat.Converters
{
    public class MultiStringLengthToBooleanConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            foreach (var value in values)
            {
                if (string.IsNullOrEmpty(value.ToString()))
                {
                    return false;
                }
            }
            return true && (bool)values[values.Length - 1];
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
