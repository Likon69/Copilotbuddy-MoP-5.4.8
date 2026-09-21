using System.Xml.Linq;

namespace Styx.CommonBot.Profiles.Quest.Order
{
    public class ClearGrindAreaNode : OrderNode
    {
        public ClearGrindAreaNode() : base(OrderNodeType.ClearGrindArea)
        {

        }

        public static new ClearGrindAreaNode FromXml(XElement element)
        {
            return new ClearGrindAreaNode();
        }

        public override string ToString()
        {
            return "[ClearGrindAreaNode]";
        }
    }
}
