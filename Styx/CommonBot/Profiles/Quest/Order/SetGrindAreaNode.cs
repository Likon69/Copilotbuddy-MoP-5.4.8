using System.Xml.Linq;
using Styx.CommonBot.AreaManagement;

namespace Styx.CommonBot.Profiles.Quest.Order
{
    public class SetGrindAreaNode : OrderNode
    {
        public SetGrindAreaNode(GrindArea area) : base(OrderNodeType.SetGrindArea)
        {
            Area = area;
        }

        public GrindArea Area { get; private set; }

        public static new SetGrindAreaNode FromXml(XElement element)
        {
            XElement grindAreaElem = ProfileHelper.FindElement(element, true, "grindarea");
            if (grindAreaElem == null)
                throw new ProfileMissingElementException("GrindArea", element);

            GrindArea grindArea = GrindArea.FromXML(grindAreaElem);
            return new SetGrindAreaNode(grindArea);
        }

        public override string ToString()
        {
            return string.Format("[SetGrindAreaNode Area: {0}]", Area);
        }
    }
}
