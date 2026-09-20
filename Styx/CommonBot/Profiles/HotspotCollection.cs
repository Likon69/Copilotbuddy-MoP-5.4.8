using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Styx.Common;
using Styx.Resources;

namespace Styx.CommonBot.Profiles
{
    public class HotspotCollection : List<WoWPoint>
    {
        public HotspotCollection()
        {
        }

        public HotspotCollection(int capacity)
            : base(capacity)
        {
        }

        public HotspotCollection(IEnumerable<WoWPoint> collection)
            : base(collection)
        {
        }

        public WoWPoint FindClosestTo(WoWPoint loc)
        {
            if (Count <= 0)
                throw new Exception(StyxResources.No_points_in_list_);

            WoWPoint closest = WoWPoint.Zero;
            float closestDist = float.MaxValue;
            for (int i = 0; i < Count; i++)
            {
                float dist = this[i].DistanceSqr(loc);
                if (dist >= closestDist)
                    continue;

                closestDist = dist;
                closest = this[i];
            }

            return closest;
        }

        /// <summary>
        /// Converts an XML element to a hotspot collection.
        /// </summary>
        /// <param name="element">The element that contains location tags.</param>
        /// <param name="elementNames">The accepted names of the location tags.</param>
        /// <returns></returns>
        internal static HotspotCollection FromXML(XElement element, params string[] elementNames)
        {
            var col = new HotspotCollection();
            foreach (XElement subElement in element.Elements())
            {
                try
                {
                    if (!elementNames.Contains(subElement.Name.ToString(), StringComparer.InvariantCultureIgnoreCase))
                        continue;

                    col.Add(ProfileHelper.ExtractLocationFromElement(subElement));
                }
                catch (ProfileException ex)
                {
                    Logging.WriteDiagnostic(ex.Message);
                }
            }

            return col;
        }
    }
}
