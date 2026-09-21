using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Styx.CommonBot.Profiles.Quest.Order
{
    public class SetBlacklistNode : OrderNode
    {
        public SetBlacklistNode(Dictionary<uint, BlacklistFlags> blacklist)
            : base(OrderNodeType.SetBlacklist)
        {
            Blacklist = blacklist;
        }

        public SetBlacklistNode(uint mobId, BlacklistFlags blackListFlags)
            : this(new Dictionary<uint, BlacklistFlags> { { mobId, blackListFlags } })
        {
        }

        public Dictionary<uint, BlacklistFlags> Blacklist { get; private set; }

        public static new SetBlacklistNode FromXml(XElement element)
        {
            return new SetBlacklistNode(Profile.ParseBlacklist(element));
        }

        public override string ToString()
        {
            return string.Format("[SetBlacklistNode: {0}]", string.Join(", ", Blacklist.Select(entry => string.Format("(Id: {0}, Flags: {1})", entry.Key, entry.Value))));
        }
    }
}
