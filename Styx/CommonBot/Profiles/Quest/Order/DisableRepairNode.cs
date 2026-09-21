using System.Xml.Linq;

namespace Styx.CommonBot.Profiles.Quest.Order
{
    public class DisableRepairNode : OrderNode
    {
        public DisableRepairNode()
            : base(OrderNodeType.DisableRepair)
        {
        }

        public static new DisableRepairNode FromXml(XElement element)
        {
            return new DisableRepairNode();
        }

        public override string ToString()
        {
            return "[DisableRepairNode]";
        }
    }
}
