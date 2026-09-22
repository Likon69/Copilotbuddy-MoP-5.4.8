using System;
using System.Globalization;
using System.Windows.Data;

namespace CopilotBuddy
{
    public class LogicalAndConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool result = true;
            foreach (object value in values)
            {
                if (value is bool)
                    result = result && (bool)value;
            }

            return result;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
