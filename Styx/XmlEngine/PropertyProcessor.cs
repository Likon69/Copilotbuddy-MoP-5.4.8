using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

namespace Styx.XmlEngine
{
    internal class PropertyProcessor
    {
        private XmlEngine Engine { get; set; }

        public PropertyProcessor(XmlEngine engine)
        {
            Engine = engine;
        }

        private bool SupportsHexNotation(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
            default:
                return false;
            case TypeCode.SByte:
            case TypeCode.Byte:
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
            case TypeCode.UInt32:
            case TypeCode.Int64:
            case TypeCode.UInt64:
                return true;
            }
        }

        private object HandleHexidecmalNotation(Type type, string value)
        {
            value = value.Replace("0x", "");
            return Type.GetTypeCode(type) switch
            {
                TypeCode.SByte => sbyte.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                TypeCode.Byte => byte.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                TypeCode.Int16 => short.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                TypeCode.UInt16 => ushort.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                TypeCode.Int32 => int.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                TypeCode.UInt32 => uint.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                TypeCode.Int64 => long.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                TypeCode.UInt64 => ulong.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        private object GetSimpleValue(Type property, string value)
        {
            if (property.IsEnum)
            {
                return Enum.Parse(property, value, ignoreCase: true);
            }
            if (value.Trim().StartsWith("0x") && SupportsHexNotation(property))
            {
                return HandleHexidecmalNotation(property, value.Trim());
            }
            return Convert.ChangeType(value, property, CultureInfo.InvariantCulture);
        }

        public void Process(PropertyInfo property, XAttribute attribute, object instance)
        {
            try
            {
                object value = GetSimpleValue(property.PropertyType, attribute.Value.Trim());
                property.SetValue(instance, value, null);
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message + " - Line: " + (((IXmlLineInfo)attribute).HasLineInfo() ? ((IXmlLineInfo)attribute).LineNumber : 0));
            }
        }

        public void Process(PropertyInfo property, XElement element, object instance)
        {
            object value;
            if (IsGenericBodyProperty(property))
            {
                if (!Engine.TypeMap.ContainsKey(element.Name.LocalName))
                {
                    throw new Exception(string.Concat("Element ", element.Name.LocalName, " is not supported. Please check your XML and try again. (", element, ") Line ", ((IXmlLineInfo)element).HasLineInfo() ? ((IXmlLineInfo)element).LineNumber : (-1)));
                }
                object item = Activator.CreateInstance(Engine.TypeMap[element.Name.LocalName], nonPublic: true);
                Engine.Load(item, element);
                value = Activator.CreateInstance(property.PropertyType, nonPublic: true) as IList;
                (value as IList).Add(item);
            }
            else if (element.HasElements)
            {
                value = GetElementValueAsCollection(element, property);
            }
            else if (Engine.TypeMap.ContainsKey(element.Name.LocalName))
            {
                value = Activator.CreateInstance(Engine.TypeMap[element.Name.LocalName], nonPublic: true);
                Engine.Load(value, element);
            }
            else
            {
                value = GetElementValueAsType(element, property.PropertyType);
            }
            object currentValue = property.GetValue(instance, null);
            if (currentValue != null && value is IList)
            {
                IList currentList = property.GetValue(instance, null) as IList;
                IList list = value as IList;
                foreach (object item in list)
                {
                    currentList.Add(item);
                }
                return;
            }
            property.SetValue(instance, value, null);
        }

        private object GetElementValueAsType(XElement element, Type asType)
        {
            if (Engine.TypeMap.ContainsValue(asType))
            {
                object value = Activator.CreateInstance(asType, nonPublic: true);
                Engine.Load(value, element);
                return value;
            }
            if (Engine.TypeMap.ContainsKey(element.Name.LocalName) && Engine.TypeMap[element.Name.LocalName].IsSubclassOf(asType))
            {
                object value = Activator.CreateInstance(Engine.TypeMap[element.Name.LocalName], nonPublic: true);
                Engine.Load(value, element);
                return value;
            }
            try
            {
                if (string.IsNullOrWhiteSpace(element.Value))
                {
                    return null;
                }
                return GetSimpleValue(asType, element.Value);
            }
            catch (Exception exception)
            {
                throw new Exception(string.Concat(exception.Message, " at line  (", element, ") Line ", ((IXmlLineInfo)element).HasLineInfo() ? ((IXmlLineInfo)element).LineNumber : (-1)));
            }
        }

        private object GetElementValueAsCollection(XElement element, PropertyInfo property)
        {
            if (!property.PropertyType.IsArray && !(property.PropertyType.GetInterface(typeof(IList).Name) != null))
            {
                return null;
            }
            Type collectionType = (property.PropertyType.HasElementType ? property.PropertyType.GetElementType() : property.PropertyType.GetGenericArguments().First());
            List<object> values = (from childElement in element.Elements()
                select GetElementValueAsType(childElement, collectionType)).ToList();
            if (property.PropertyType.IsArray)
            {
                Array array = Array.CreateInstance(collectionType, values.Count);
                for (int index = 0; index < values.Count; index++)
                {
                    array.SetValue(values[index], index);
                }
                return array;
            }
            IList list = Activator.CreateInstance(property.PropertyType) as IList;
            foreach (object value in values)
            {
                list.Add(value);
            }
            if (values.Count == 0)
            {
                object value = GetElementValueAsType(element, collectionType);
                list.Add(value);
            }
            return list;
        }

        private static bool IsGenericBodyProperty(PropertyInfo property)
        {
            return property.GetCustomAttributes(typeof(XmlElementAttribute), inherit: true).Any((object attributeObject) => attributeObject is INamedAttribute namedAttribute && namedAttribute.Name == "_XMLENGINE_BODY_");
        }
    }
}
