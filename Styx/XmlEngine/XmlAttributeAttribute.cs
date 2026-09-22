using System;

namespace Styx.XmlEngine
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
    public sealed class XmlAttributeAttribute : Attribute, INamedAttribute
    {
        public XmlAttributeAttribute(string attributeName, bool shouldSave = true)
        {
            Name = attributeName;
            ShouldSave = shouldSave;
        }

        public string Name { get; set; }
        public bool ShouldSave { get; set; }
    }
}
