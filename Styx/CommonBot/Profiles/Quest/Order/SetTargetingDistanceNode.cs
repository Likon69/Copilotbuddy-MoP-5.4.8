using System;
using System.Xml.Linq;

namespace Styx.CommonBot.Profiles.Quest.Order
{
    public class SetTargetingDistanceNode : OrderNode
    {
        public SetTargetingDistanceNode(double? targetingDistance, XElement element)
            : base(OrderNodeType.SetTargetingDistance, element)
        {
            TargetingDistance = targetingDistance;
        }

        public double? TargetingDistance { get; private set; }

        public static new SetTargetingDistanceNode FromXml(XElement element)
        {
            XAttribute valueAttrib = ProfileHelper.FindAttribute(element, true, "value");
            if (valueAttrib == null)
            {
                throw new ProfileMissingAttributeException<double>("Value", element);
            }
            double value;
            if (double.TryParse(valueAttrib.Value, out value))
            {
                return new SetTargetingDistanceNode(value, element);
            }
            if (string.Equals(valueAttrib.Value, "null", StringComparison.InvariantCultureIgnoreCase))
            {
                return new SetTargetingDistanceNode(null, element);
            }
            throw new ProfileAttributeExpectedException<double>(valueAttrib, "null");
        }
    }
}
