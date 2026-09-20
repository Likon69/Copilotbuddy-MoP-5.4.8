using System;

namespace Styx.Helpers
{
    /// <summary>
    /// Marks a property as a setting, optionally providing an explanation and element name for the output XML file.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SettingAttribute : Attribute
    {
        public string ElementName;
        public string Explanation;

        public SettingAttribute()
        {
        }

        public SettingAttribute(string elementName)
            : this(elementName, null)
        {
        }

        public SettingAttribute(string elementName, string explanation)
        {
            ElementName = elementName;
            Explanation = explanation;
        }
    }
}
