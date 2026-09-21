using System.Xml.Linq;

namespace Styx.CommonBot.Profiles.Quest.Order
{
    public class ClearVendorNode : OrderNode
    {
        public ClearVendorNode() : base(OrderNodeType.ClearVendor)
        {

        }

        public static new ClearVendorNode FromXml(XElement element)
        {
            return new ClearVendorNode();
        }

        public override string ToString()
        {
            return "[ClearVendorNode]";
        }
    }
}
