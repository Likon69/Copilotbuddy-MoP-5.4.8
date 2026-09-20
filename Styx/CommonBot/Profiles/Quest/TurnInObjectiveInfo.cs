using System.Xml.Linq;

namespace Styx.CommonBot.Profiles.Quest
{
    public class TurnInObjectiveInfo : ObjectiveInfo
    {
        public TurnInObjectiveInfo(WoWPoint npcLocation)
            : base(ObjectiveType.TurnIn)
        {
            Location = npcLocation;
        }

        public WoWPoint Location { get; private set; }

        internal new static TurnInObjectiveInfo FromXML(XElement element)
        {
            WoWPoint point = ProfileHelper.ExtractLocationFromElement(element);

            return new TurnInObjectiveInfo(point);
        }
    }
}
