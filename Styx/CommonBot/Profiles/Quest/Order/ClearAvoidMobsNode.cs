using System.Xml.Linq;

namespace Styx.CommonBot.Profiles.Quest.Order
{
    public class ClearAvoidMobsNode : OrderNode
    {
        public ClearAvoidMobsNode()
            : base(OrderNodeType.ClearAvoidMobs)
        {
        }

        public static new ClearAvoidMobsNode FromXml(XElement element)
        {
            return new ClearAvoidMobsNode();
        }

        public override string ToString()
        {
            return "[ClearAvoidMobsNode]";
        }
    }
}
