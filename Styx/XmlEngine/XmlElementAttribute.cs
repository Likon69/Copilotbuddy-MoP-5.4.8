using System;

namespace Styx.XmlEngine
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property,
        Inherited = true, AllowMultiple = true)]
    public sealed class XmlElementAttribute : Attribute, INamedAttribute
    {
        public XmlElementAttribute(string elementName)
        {
            Name = elementName;
        }

        public string Name { get; set; }
    }
}
