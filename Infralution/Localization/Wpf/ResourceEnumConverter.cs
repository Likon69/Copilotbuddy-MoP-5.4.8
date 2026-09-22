using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Windows.Data;

namespace Infralution.Localization.Wpf
{
    public class ResourceEnumConverter : EnumConverter, IValueConverter
    {
        private Dictionary<CultureInfo, Class874> dictionary_0 = new Dictionary<CultureInfo, Class874>();
        private ResourceManager resourceManager_0;
        private bool bool_0;
        private Array array_0;

        private Class874 method_0(CultureInfo cultureInfo_0)
        {
            Class874 @class = null;
            if (cultureInfo_0 == null)
            {
                cultureInfo_0 = CultureInfo.CurrentCulture;
            }
            if (!dictionary_0.TryGetValue(cultureInfo_0, out @class))
            {
                @class = new Class874();
                foreach (object obj in base.GetStandardValues())
                {
                    string text = method_1(cultureInfo_0, obj);
                    if (text != null)
                    {
                        @class.Add(text, obj);
                    }
                }
                dictionary_0.Add(cultureInfo_0, @class);
            }
            return @class;
        }

        protected virtual string GetResourceName(object value)
        {
            Type type = value.GetType();
            return string.Format("{0}_{1}", type.Name, value.ToString());
        }

        private string method_1(CultureInfo cultureInfo_0, object object_0)
        {
            string resourceName = GetResourceName(object_0);
            string text = resourceManager_0.GetString(resourceName, cultureInfo_0);
            if (text == null)
            {
                text = resourceName;
            }
            return text;
        }

        private bool method_2(ulong ulong_0)
        {
            if (ulong_0 <= 1UL && ulong_0 >= 0UL)
            {
                switch ((int)ulong_0)
                {
                    case 0:
                        return false;
                    case 1:
                        return true;
                }
            }
            return (ulong_0 & (ulong_0 - 1UL)) == 0UL;
        }

        private string method_3(CultureInfo cultureInfo_0, object object_0)
        {
            if (Enum.IsDefined(object_0.GetType(), object_0))
            {
                return method_1(cultureInfo_0, object_0);
            }
            ulong num = (ulong)Convert.ToUInt32(object_0);
            string text = null;
            foreach (object obj in array_0)
            {
                ulong num2 = (ulong)Convert.ToUInt32(obj);
                if (method_2(num2) && (num2 & num) == num2)
                {
                    string text2 = method_1(cultureInfo_0, obj);
                    if (text == null)
                    {
                        text = text2;
                    }
                    else
                    {
                        text = string.Format("{0}, {1}", text, text2);
                    }
                }
            }
            return text;
        }

        private object method_4(CultureInfo cultureInfo_0, string string_0)
        {
            Class874 @class = method_0(cultureInfo_0);
            object obj = null;
            @class.TryGetValue(string_0, out obj);
            return obj;
        }

        private object method_5(CultureInfo cultureInfo_0, string string_0)
        {
            Class874 @class = method_0(cultureInfo_0);
            string[] array = string_0.Split(new char[] { ',' });
            ulong num = 0UL;
            foreach (string text in array)
            {
                object obj = null;
                string text2 = text.Trim();
                if (!@class.TryGetValue(text2, out obj))
                {
                    return null;
                }
                num |= (ulong)Convert.ToUInt32(obj);
            }
            return Enum.ToObject(base.EnumType, num);
        }

        public ResourceEnumConverter(Type type, ResourceManager resourceManager)
            : base(type)
        {
            resourceManager_0 = resourceManager;
            object[] customAttributes = type.GetCustomAttributes(typeof(FlagsAttribute), true);
            bool_0 = customAttributes.Length > 0;
            if (bool_0)
            {
                array_0 = Enum.GetValues(type);
            }
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (culture == null)
            {
                culture = CultureInfo.CurrentCulture;
            }
            if (value is string)
            {
                object obj = bool_0 ? method_5(culture, (string)value) : method_4(culture, (string)value);
                if (obj == null)
                {
                    obj = base.ConvertFrom(context, culture, value);
                }
                return obj;
            }
            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (culture == null)
            {
                culture = CultureInfo.CurrentCulture;
            }
            if (value != null && destinationType == typeof(string))
            {
                return bool_0 ? method_3(culture, value) : method_1(culture, value);
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

        public static string ConvertToString(Enum value)
        {
            TypeConverter converter = TypeDescriptor.GetConverter(value.GetType());
            return converter.ConvertToString(value);
        }

        public static List<KeyValuePair<Enum, string>> GetValues(Type enumType, CultureInfo culture)
        {
            List<KeyValuePair<Enum, string>> list = new List<KeyValuePair<Enum, string>>();
            TypeConverter converter = TypeDescriptor.GetConverter(enumType);
            foreach (object obj in Enum.GetValues(enumType))
            {
                Enum @enum = (Enum)obj;
                KeyValuePair<Enum, string> keyValuePair = new KeyValuePair<Enum, string>(@enum, converter.ConvertToString(null, culture, @enum));
                list.Add(keyValuePair);
            }
            return list;
        }

        public static List<KeyValuePair<Enum, string>> GetValues(Type enumType)
        {
            return GetValues(enumType, CultureInfo.CurrentUICulture);
        }

        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ConvertTo(null, culture, value, targetType);
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ConvertFrom(null, culture, value);
        }

        private class Class874 : Dictionary<string, object>
        {
        }
    }
}
