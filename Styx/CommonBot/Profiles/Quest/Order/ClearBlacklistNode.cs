using System.Xml.Linq;

namespace Styx.CommonBot.Profiles.Quest.Order
{
    public class ClearBlacklistNode : OrderNode
    {
        public ClearBlacklistNode()
            : base(OrderNodeType.ClearBlacklist)
        {
        }

        public static new ClearBlacklistNode FromXml(XElement element)
        {
            return new ClearBlacklistNode();
        }

        public override string ToString()
        {
            return "[ClearBlacklistNode]";
        }
    }
}
