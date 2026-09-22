#define TRACE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace Styx.XmlEngine
{
    public class XmlEngine
    {
        public const string GENERIC_BODY = "_XMLENGINE_BODY_";

        public Dictionary<string, Type> TypeMap { get; private set; }

        private PropertyProcessor PropertyProcessor { get; set; }

        public XmlEngine(string namespaceRestriction = null, bool includeChildNamespaces = false)
        {
            TypeMap = new Dictionary<string, Type>();
            ReloadTypeMap(namespaceRestriction, includeChildNamespaces);
            PropertyProcessor = new PropertyProcessor(this);
        }

        public void Load(object obj, XElement element)
        {
            if (element == null || obj == null)
            {
                return;
            }
            if (element.HasAttributes)
            {
                Dictionary<string, PropertyInfo> attributes = GetObjectAttributePropMap<XmlAttributeAttribute>(obj);
                foreach (XAttribute attribute in element.Attributes())
                {
                    if (attributes.ContainsKey(attribute.Name.LocalName))
                    {
                        PropertyInfo property = attributes[attribute.Name.LocalName];
                        PropertyProcessor.Process(property, attribute, obj);
                    }
                }
            }
            if (!element.HasElements)
            {
                return;
            }
            Dictionary<string, PropertyInfo> elements = GetObjectAttributePropMap<XmlElementAttribute>(obj);
            foreach (XElement childElement in element.Elements())
            {
                if (!elements.ContainsKey(childElement.Name.LocalName) && !elements.ContainsKey("_XMLENGINE_BODY_"))
                {
                    Trace.Write("Element " + childElement.Name.LocalName + " is not supported yet!");
                }
                else if (elements.ContainsKey("_XMLENGINE_BODY_") && elements.Count == 1)
                {
                    PropertyProcessor.Process(elements["_XMLENGINE_BODY_"], childElement, obj);
                }
                else
                {
                    PropertyProcessor.Process(elements[childElement.Name.LocalName], childElement, obj);
                }
            }
        }

        public XElement Save(object obj, string @namespace = null, XElement parentElement = null)
        {
            Dictionary<string, PropertyInfo> attributes = GetObjectAttributePropMap<XmlAttributeAttribute>(obj);
            Dictionary<string, PropertyInfo> elements = GetObjectAttributePropMap<XmlElementAttribute>(obj);
            XmlElementAttribute elementAttribute = obj.GetType().GetCustomAttributes(typeof(XmlElementAttribute), inherit: true).First() as XmlElementAttribute;
            XElement element = new XElement(elementAttribute.Name);
            if (@namespace != null)
            {
                element.Name = XName.Get(elementAttribute.Name, @namespace);
            }
            foreach (KeyValuePair<string, PropertyInfo> attribute in attributes)
            {
                object value = attribute.Value.GetValue(obj, null);
                if (value != null && !value.Equals(GetDefault(value.GetType())))
                {
                    XAttribute content = new XAttribute(attribute.Key, attribute.Value.GetValue(obj, null));
                    element.Add(content);
                }
            }
            foreach (KeyValuePair<string, PropertyInfo> childProperty in elements)
            {
                object value = childProperty.Value.GetValue(obj, null);
                if (value == null)
                {
                    continue;
                }
                if (!TypeMap.ContainsKey(childProperty.Key) && !TypeMap.ContainsValue(value.GetType()))
                {
                    if (childProperty.Value.PropertyType.GetInterface(typeof(IEnumerable).Name, ignoreCase: true) != null && childProperty.Value.PropertyType != typeof(string))
                    {
                        XElement collectionElement = new XElement(childProperty.Key);
                        IEnumerable collection = value as IEnumerable;
                        Type collectionType = (childProperty.Value.PropertyType.HasElementType ? childProperty.Value.PropertyType.GetElementType() : childProperty.Value.PropertyType.GetGenericArguments().First());
                        bool customType = TypeMap.ContainsValue(collectionType) || childProperty.Value.GetCustomAttributes(typeof(XmlElementAttribute), inherit: true).Any((object attributeObject) => attributeObject is INamedAttribute namedAttribute && namedAttribute.Name == "_XMLENGINE_BODY_");
                        foreach (object item in collection)
                        {
                            if (item != null)
                            {
                                if (!customType && !TypeMap.ContainsValue(item.GetType()))
                                {
                                    collectionElement.Add(new XElement("Value", item));
                                }
                                else if (childProperty.Key == "_XMLENGINE_BODY_")
                                {
                                    element.Add(Save(item, @namespace, element));
                                }
                                else
                                {
                                    collectionElement.Add(Save(item, @namespace, element));
                                }
                            }
                        }
                        if (childProperty.Key != "_XMLENGINE_BODY_")
                        {
                            element.Add(collectionElement);
                        }
                    }
                    else
                    {
                        element.Add(new XElement(childProperty.Key, value));
                    }
                }
                else
                {
                    XElement content = Save(value, @namespace, element);
                    element.Add(content);
                }
            }
            return element;
        }

        private static Dictionary<string, PropertyInfo> GetObjectAttributePropMap<T>(object obj) where T : INamedAttribute
        {
            Dictionary<string, PropertyInfo> propertiesByName = new Dictionary<string, PropertyInfo>();
            PropertyInfo[] properties = obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (PropertyInfo property in properties)
            {
                foreach (INamedAttribute attribute in property.GetCustomAttributes(typeof(T), inherit: true).OfType<INamedAttribute>())
                {
                    propertiesByName.Add(attribute.Name, property);
                }
            }
            return propertiesByName;
        }

        private void ReloadTypeMap(string namespaceRestriction, bool includeChildNamespaces)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                try
                {
                    Type[] types = assembly.GetTypes();
                    foreach (Type type in types)
                    {
                        if (namespaceRestriction != null)
                        {
                            if (includeChildNamespaces)
                            {
                                if (!type.Namespace.StartsWith(namespaceRestriction))
                                {
                                    continue;
                                }
                            }
                            else if (type.Namespace != namespaceRestriction)
                            {
                                continue;
                            }
                        }
                        if (!type.IsClass)
                        {
                            continue;
                        }
                        foreach (XmlElementAttribute attribute in type.GetCustomAttributes(typeof(XmlElementAttribute), inherit: true).OfType<XmlElementAttribute>())
                        {
                            if (!TypeMap.ContainsKey(attribute.Name))
                            {
                                TypeMap.Add(attribute.Name, type);
                            }
                        }
                    }
                }
                catch (ReflectionTypeLoadException exception)
                {
                    Trace.WriteLine(exception.Message);
                    Trace.WriteLine("Loader exceptions:");
                    Exception[] loaderExceptions = exception.LoaderExceptions;
                    foreach (Exception loaderException in loaderExceptions)
                    {
                        Trace.WriteLine(loaderException.ToString());
                    }
                }
            }
        }

        private static object GetDefault(Type type)
        {
            if (!(type == null) && type.IsValueType && !(type == typeof(void)))
            {
                if (type.ContainsGenericParameters)
                {
                    throw new ArgumentException(string.Concat("{", MethodBase.GetCurrentMethod(), "} Error:\n\nThe supplied value type <", type, "> contains generic parameters, so the default value cannot be retrieved"));
                }
                if (!type.IsPrimitive && type.IsNotPublic)
                {
                    throw new ArgumentException(string.Concat("{", MethodBase.GetCurrentMethod(), "} Error:\n\nThe supplied value type <", type, "> is not a publicly-visible type, so the default value cannot be retrieved"));
                }
                try
                {
                    return Activator.CreateInstance(type);
                }
                catch (Exception exception)
                {
                    throw new ArgumentException(string.Concat("{", MethodBase.GetCurrentMethod(), "} Error:\n\nThe Activator.CreateInstance method could not create a default instance of the supplied value type <", type, "> (Inner Exception message: \"", exception.Message, "\")"), exception);
                }
            }
            return null;
        }
    }
}
