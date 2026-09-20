using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Styx.Common;

namespace Styx.CommonBot.Profiles.Quest
{
    public class CollectFromCollection : List<CollectFrom>
    {
        public CollectFromCollection()
        {
        }

        public CollectFromCollection(int capacity)
            : base(capacity)
        {
        }

        public CollectFromCollection(IEnumerable<CollectFrom> collection)
            : base(collection)
        {
        }

        public bool ContainsMob(uint mobID)
        {
            for (int i = 0; i < Count; i++)
            {
                if (this[i].Type == CollectFromType.Mob && this[i].ID == mobID)
                    return true;
            }

            return false;
        }

        public bool ContainsGameObject(uint gameObjectID)
        {
            for (int i = 0; i < Count; i++)
            {
                if (this[i].Type == CollectFromType.GameObject && this[i].ID == gameObjectID)
                    return true;
            }

            return false;
        }

        public bool ContainsVendor(uint vendorID)
        {
            for (int i = 0; i < Count; i++)
            {
                if (this[i].Type == CollectFromType.Vendor && this[i].ID == vendorID)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Converts an XML element to a kill mob/game object collection.
        /// </summary>
        /// <param name="element">The element that contains kill mob/game object tags.</param>
        /// <param name="elementNames">The accepted names of the kill mob/game object tags.</param>
        /// <returns></returns>
        internal static CollectFromCollection FromXML(XElement element, params string[] elementNames)
        {
            var col = new CollectFromCollection();
            foreach (XElement subElement in element.Elements())
            {
                try
                {
                    if (!elementNames.Contains(subElement.Name.ToString(), StringComparer.InvariantCultureIgnoreCase))
                        continue;

                    col.Add(CollectFrom.FromXML(subElement));
                }
                catch (ProfileException ex)
                {
                    Logging.WriteException(ex);
                }
            }

            return col;
        }
    }
}
