using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Styx.Common;

namespace Styx.CommonBot.Profiles.Quest.Order
{
    public class SetAvoidMobsNode : OrderNode
    {
        public SetAvoidMobsNode(DualHashSet<uint, string> avoidMobs)
            : base(OrderNodeType.SetAvoidMobs)
        {
            AvoidMobs = avoidMobs;
        }

        public SetAvoidMobsNode(uint mobId, string mobName)
            : this(new DualHashSet<uint, string> { mobId, mobName })
        {
        }

        public DualHashSet<uint, string> AvoidMobs { get; private set; }

        public static new SetAvoidMobsNode FromXml(XElement element)
        {
            return new SetAvoidMobsNode(Profile.ParseAvoidMobs(element));
        }

        public override string ToString()
        {
            return string.Format("[SetAvoidMobsNode: {0}]", string.Join(", ", AvoidMobs.Select(avoidMob => avoidMob.ToString())));
        }
    }
}
