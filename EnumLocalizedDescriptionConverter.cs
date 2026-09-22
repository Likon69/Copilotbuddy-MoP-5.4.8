using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Data;

namespace CopilotBuddy
{
    public class EnumLocalizedDescriptionConverter : IValueConverter
    {
        private string GetLocalizedDescription(Enum enumObj)
        {
            FieldInfo field = enumObj.GetType().GetField(enumObj.ToString());
            object[] attributes = field.GetCustomAttributes(typeof(DescriptionAttribute), true);
            return attributes.Length == 0
                ? enumObj.ToString()
                : ((DescriptionAttribute)attributes[0]).Description;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return GetLocalizedDescription((Enum)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Enum.GetValues(targetType).Cast<Enum>()
                .FirstOrDefault(item => GetLocalizedDescription(item).Equals(value));
        }
    }
}
