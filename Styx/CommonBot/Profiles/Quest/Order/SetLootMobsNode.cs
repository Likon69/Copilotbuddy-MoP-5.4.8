using System;
using System.Xml.Linq;

namespace Styx.CommonBot.Profiles.Quest.Order
{
    public class SetLootMobsNode : OrderNode
    {
        public SetLootMobsNode(bool? lootMobs, XElement element)
            : base(OrderNodeType.SetLootMobs, element)
        {
            LootMobs = lootMobs;
        }

        public bool? LootMobs { get; private set; }

        public static new SetLootMobsNode FromXml(XElement element)
        {
            XAttribute valueAttrib = ProfileHelper.FindAttribute(element, true, "value");
            if (valueAttrib == null)
            {
                throw new ProfileMissingAttributeException<bool?>("Value", element);
            }
            bool value;
            if (bool.TryParse(valueAttrib.Value, out value))
            {
                return new SetLootMobsNode(value, element);
            }
            if (string.Equals(valueAttrib.Value, "null", StringComparison.InvariantCultureIgnoreCase))
            {
                return new SetLootMobsNode(null, element);
            }
            throw new ProfileAttributeExpectedException<bool?>(valueAttrib, "null");
        }
    }
}
