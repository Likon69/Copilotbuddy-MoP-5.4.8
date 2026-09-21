using System.Xml.Linq;

namespace Styx.CommonBot.Profiles.Quest.Order
{
    public class EnableRepairNode : OrderNode
    {
        public EnableRepairNode()
            : base(OrderNodeType.EnableRepair)
        {
        }

        public static new EnableRepairNode FromXml(XElement element)
        {
            return new EnableRepairNode();
        }

        public override string ToString()
        {
            return "[EnableRepairNode]";
        }
    }
}
