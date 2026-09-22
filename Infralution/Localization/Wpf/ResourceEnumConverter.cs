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
        private Dictionary<CultureInfo, LookupTable> _lookupTables = new Dictionary<CultureInfo, LookupTable>();
        private ResourceManager _resourceManager;
        private bool _isFlagEnum;
        private Array _flagValues;

        private LookupTable GetLookupTable(CultureInfo culture)
        {
            LookupTable lookupTable = null;
            if (culture == null)
            {
                culture = CultureInfo.CurrentCulture;
            }
            if (!_lookupTables.TryGetValue(culture, out lookupTable))
            {
                lookupTable = new LookupTable();
                foreach (object obj in base.GetStandardValues())
                {
                    string text = GetValueText(culture, obj);
                    if (text != null)
                    {
                        lookupTable.Add(text, obj);
                    }
                }
                _lookupTables.Add(culture, lookupTable);
            }
            return lookupTable;
        }

        protected virtual string GetResourceName(object value)
        {
            Type type = value.GetType();
            return string.Format("{0}_{1}", type.Name, value.ToString());
        }

        private string GetValueText(CultureInfo culture, object value)
        {
            string resourceName = GetResourceName(value);
            string text = _resourceManager.GetString(resourceName, culture);
            if (text == null)
            {
                text = resourceName;
            }
            return text;
        }

        private bool IsSingleBitValue(ulong value)
        {
            if (value <= 1UL && value >= 0UL)
            {
                switch ((int)value)
                {
                    case 0:
                        return false;
                    case 1:
                        return true;
                }
            }
            return (value & (value - 1UL)) == 0UL;
        }

        private string GetFlagValueText(CultureInfo culture, object value)
        {
            if (Enum.IsDefined(value.GetType(), value))
            {
                return GetValueText(culture, value);
            }
            ulong num = (ulong)Convert.ToUInt32(value);
            string text = null;
            foreach (object obj in _flagValues)
            {
                ulong num2 = (ulong)Convert.ToUInt32(obj);
                if (IsSingleBitValue(num2) && (num2 & num) == num2)
                {
                    string text2 = GetValueText(culture, obj);
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

        private object GetValue(CultureInfo culture, string text)
        {
            LookupTable lookupTable = GetLookupTable(culture);
            object obj = null;
            lookupTable.TryGetValue(text, out obj);
            return obj;
        }

        private object GetFlagValue(CultureInfo culture, string text)
        {
            LookupTable lookupTable = GetLookupTable(culture);
            string[] array = text.Split(new char[] { ',' });
            ulong num = 0UL;
            foreach (string textValue in array)
            {
                object obj = null;
                string trimmedTextValue = textValue.Trim();
                if (!lookupTable.TryGetValue(trimmedTextValue, out obj))
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
            _resourceManager = resourceManager;
            object[] customAttributes = type.GetCustomAttributes(typeof(FlagsAttribute), true);
            _isFlagEnum = customAttributes.Length > 0;
            if (_isFlagEnum)
            {
                _flagValues = Enum.GetValues(type);
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
                object obj = _isFlagEnum ? GetFlagValue(culture, (string)value) : GetValue(culture, (string)value);
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
                return _isFlagEnum ? GetFlagValueText(culture, value) : GetValueText(culture, value);
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

        private class LookupTable : Dictionary<string, object>
        {
        }
    }
}
