using System;
using System.Globalization;
using System.Windows.Data;

namespace PDS.Witsml.Studio.Converters
{
    public class BooleanInverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = false;
            if ((value == null ? true : !bool.TryParse(value.ToString(), out flag)))
            {
                return Binding.DoNothing;
            }
            return !flag;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
