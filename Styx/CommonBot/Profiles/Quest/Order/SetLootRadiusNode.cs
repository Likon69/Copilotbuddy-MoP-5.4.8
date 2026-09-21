using System;
using System.Xml.Linq;

namespace Styx.CommonBot.Profiles.Quest.Order
{
    public class SetLootRadiusNode : OrderNode
    {
        public SetLootRadiusNode(double? lootRadius, XElement element)
            : base(OrderNodeType.SetLootRadius, element)
        {
            LootRadius = lootRadius;
        }

        public double? LootRadius { get; private set; }

        public static new SetLootRadiusNode FromXml(XElement element)
        {
            XAttribute valueAttrib = ProfileHelper.FindAttribute(element, true, "value");
            if (valueAttrib == null)
            {
                throw new ProfileMissingAttributeException<double>("Value", element);
            }
            double value;
            if (double.TryParse(valueAttrib.Value, out value))
            {
                return new SetLootRadiusNode(value, element);
            }
            if (string.Equals(valueAttrib.Value, "null", StringComparison.InvariantCultureIgnoreCase))
            {
                return new SetLootRadiusNode(null, element);
            }
            throw new ProfileAttributeExpectedException<double>(valueAttrib, "null");
        }
    }
}
