using System;
using System.Xml.Linq;

namespace Styx.CommonBot.Profiles.Quest.Order
{
    public class SetNavTypeNode : OrderNode
    {
        public SetNavTypeNode(NavType navType, XElement element)
            : base(OrderNodeType.SetNavType, element)
        {
            NavType = navType;
        }

        public NavType NavType { get; private set; }

        public static new SetNavTypeNode FromXml(XElement element)
        {
            XAttribute valueAttrib = ProfileHelper.FindAttribute(element, true, "value");
            if (valueAttrib == null)
            {
                throw new ProfileMissingAttributeException<string>("Value", element);
            }
            NavType navType;
            if (!Enum.TryParse<NavType>(valueAttrib.Value, true, out navType))
            {
                throw new ProfileAttributeExpectedException(valueAttrib, Enum.GetNames(typeof(NavType)));
            }
            return new SetNavTypeNode(navType, element);
        }
    }
}
