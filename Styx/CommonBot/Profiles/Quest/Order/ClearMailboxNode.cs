using System.Xml.Linq;

namespace Styx.CommonBot.Profiles.Quest.Order
{
    public class ClearMailboxNode : OrderNode
    {
        public ClearMailboxNode() : base(OrderNodeType.ClearMailbox)
        {

        }

        public static new ClearMailboxNode FromXml(XElement element)
        {
            return new ClearMailboxNode();
        }

        public override string ToString()
        {
            return "[ClearMailboxNode]";
        }
    }
}
