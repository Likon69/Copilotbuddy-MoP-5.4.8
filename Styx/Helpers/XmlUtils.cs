using System;
using System.Linq;
using System.Xml.Linq;

namespace Styx.Helpers
{
    public static class XmlUtils
    {
        public static string FindAttributeValue(XElement element, string name, bool caseSensitive)
        {
            var attributes = element.Attributes();
            XAttribute attribute = caseSensitive ? attributes.FirstOrDefault(a => a.Name == name) : attributes.FirstOrDefault(a => a.Name.ToString().ToLower() == name.ToLower());
            return attribute != null ? attribute.Value : null;
        }

        /// <summary>Gets an integer value from the specified element, using the provided attribute name.</summary>
        /// <remarks>Created 1/29/2011.</remarks>
        /// <param name="element">The element.</param>
        /// <param name="name">The attribute name to get the value from.</param>
        /// <param name="minValue">The minimum value.</param>
        /// <param name="maxValue">The maximum value.</param>
        /// <param name="value">[out] The value of the attribute.</param>
        /// <returns>true if it succeeds, false if it fails.</returns>
        public static bool GetInt32Attribute(XElement element, string name, int minValue, int maxValue, out int value)
        {
            value = default(int);

            string attributeValue = FindAttributeValue(element, name, false);

            if (string.IsNullOrEmpty(attributeValue))
                return false;

            int realValue;

            if (!int.TryParse(attributeValue, out realValue))
                return false;

            if (realValue >= minValue && realValue <= maxValue)
            {
                value = realValue;
                return true;
            }

            return false;
        }
    }
}
