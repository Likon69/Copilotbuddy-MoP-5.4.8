using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using Styx.Pathing;

namespace Styx.CommonBot.Profiles
{
    internal static class ProfileHelper
    {
        public static bool ParseHeightString(float x, float y, string s, out float val)
        {
            string lower = s.ToLowerInvariant();
            switch (lower)
            {
                case "findlowest":
                case "findhighest":
                    List<float> heights = Navigator.FindHeights(x, y);
                    if (heights != null && heights.Count > 0)
                    {
                        val = lower == "findlowest" ? heights.Min() : heights.Max();
                        return true;
                    }
                    break;
            }

            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out val);
        }

        public static WoWPoint ExtractLocationFromElement(XElement element, bool throwOnMissing = false)
        {
            XAttribute xAttrib = FindAttribute(element, true, "x");
            XAttribute yAttrib = FindAttribute(element, true, "y");
            XAttribute zAttrib = FindAttribute(element, true, "z");

            if (xAttrib != null && yAttrib != null && zAttrib != null)
            {
                if (xAttrib.Value == "0" && yAttrib.Value == "0" && zAttrib.Value == "0")
                    return WoWPoint.Zero;

                float x, y, z;
                if (!float.TryParse(xAttrib.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out x))
                    throw new ProfileAttributeExpectedException<float>(xAttrib);

                if (!float.TryParse(yAttrib.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out y))
                    throw new ProfileAttributeExpectedException<float>(yAttrib);

                if (!ParseHeightString(x, y, zAttrib.Value, out z))
                    throw new ProfileAttributeExpectedException<float>(zAttrib);

                return new WoWPoint(x, y, z);
            }

            if (throwOnMissing)
                throw new ProfileMissingAttributeException<float>(xAttrib == null ? (yAttrib == null ? "Z" : "Y") : "X", element);

            return WoWPoint.Zero;
        }

        public static bool Extract<T>(XAttribute attrib, out T val)
        {
            if (attrib == null)
            {
                val = default(T);
                return false;
            }

            try
            {
                val = (T)Convert.ChangeType(attrib.Value, typeof(T));
                return true;
            }
            catch (InvalidCastException)
            {
                val = default(T);
                return false;
            }
        }

        public static XAttribute FindAttribute(XElement element, bool ignoreCase, params string[] attributeNames)
        {
            StringComparer comparer = ignoreCase ? StringComparer.InvariantCultureIgnoreCase : StringComparer.InvariantCulture;

            return element.Attributes().FirstOrDefault(attribute => attributeNames.Contains(attribute.Name.ToString(), comparer));
        }

        public static XElement FindElement(XElement element, bool ignoreCase, params string[] elementNames)
        {
            StringComparer comparer = ignoreCase ? StringComparer.InvariantCultureIgnoreCase : StringComparer.InvariantCulture;

            return element.Elements().FirstOrDefault(elm => elementNames.Contains(elm.Name.ToString(), comparer));
        }

        public static IEnumerable<XElement> FindElements(XElement parentElement, bool ignoreCase, params string[] elementNames)
        {
            StringComparer comparer = ignoreCase ? StringComparer.InvariantCultureIgnoreCase : StringComparer.InvariantCulture;

            return parentElement.Elements().Where(elm => elementNames.Contains(elm.Name.ToString(), comparer));
        }
    }
}
