using System.Xml.Linq;

namespace Styx.CommonBot.Profiles.Quest.Order
{
    public class SetUseMountNode : OrderNode
    {
        public SetUseMountNode(bool useMount, XElement element)
            : base(OrderNodeType.SetUseMount, element)
        {
            UseMount = useMount;
        }

        public bool UseMount { get; private set; }

        public static new SetUseMountNode FromXml(XElement element)
        {
            XAttribute valueAttrib = ProfileHelper.FindAttribute(element, true, "value");
            if (valueAttrib == null)
            {
                throw new ProfileMissingAttributeException<bool>("Value", element);
            }
            bool value;
            if (!bool.TryParse(valueAttrib.Value, out value))
            {
                throw new ProfileAttributeExpectedException<bool>(valueAttrib);
            }
            return new SetUseMountNode(value, element);
        }
    }
}
