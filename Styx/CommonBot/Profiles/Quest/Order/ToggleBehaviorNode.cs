using System;
using System.Xml.Linq;

namespace Styx.CommonBot.Profiles.Quest.Order
{
    public class ToggleBehaviorNode : OrderNode
    {
        public ToggleBehaviorNode(string behaviorName, bool enabled, XElement element)
            : base(OrderNodeType.ToggleBehavior, element)
        {
            BehaviorName = behaviorName;
            Enabled = enabled;
        }

        public string BehaviorName { get; private set; }

        public bool Enabled { get; private set; }

        public static new ToggleBehaviorNode FromXml(XElement element)
        {
            XAttribute nameAttrib = ProfileHelper.FindAttribute(element, true, "name", "behavior");
            string behaviorName = nameAttrib != null ? nameAttrib.Value.ToLowerInvariant() : null;
            bool enabled = element.Name.LocalName.Equals("enablebehavior", StringComparison.InvariantCultureIgnoreCase);
            return new ToggleBehaviorNode(behaviorName, enabled, element);
        }

        public override string ToString()
        {
            return string.Format("[ToggleBehaviorNode Behavior: {0} State: {1}]", BehaviorName, Enabled);
        }
    }
}
