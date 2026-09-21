using System;
using System.Globalization;
using System.Xml.Linq;
using Styx.Helpers;

using Styx.Resources;

namespace Styx.CommonBot.Profiles.Quest.Order
{
    public class MoveToNode : OrderNode
    {
        public MoveToNode(WoWPoint location, string locationName, float precision, uint questId, NavType navType) : base(OrderNodeType.MoveTo)
        {
            Location = location;
            LocationName = locationName ?? string.Format("<{0}, {1}, {2}>", location.X.ToStringInvariant(), location.Y.ToStringInvariant(), location.Z.ToStringInvariant());
            Precision = precision;
            QuestId = questId;
            NavType = navType;
        }

        public WoWPoint Location { get; private set; }
        public string LocationName { get; private set; }
        public float Precision { get; private set; }
        public uint QuestId { get; private set; }
        public NavType NavType { get; private set; }

        public override string ToString()
        {
            return string.Format("[MoveToNode Location: {0} NavType: {1}]", Location, NavType);
        }

        public static new MoveToNode FromXml(XElement element)
        {
            WoWPoint loc;
            try
            {
                loc = ProfileHelper.ExtractLocationFromElement(element);
            }
            catch (ProfileException ex)
            {
                throw new ProfileException(StyxResources.Could_not_parse_X_Y_and_Z_in__MoveToNode__, ex);
            }

            XAttribute nameAttrib = ProfileHelper.FindAttribute(element, true, "name", "destname");
            string name = nameAttrib != null ? nameAttrib.Value : null;

            float precision;
            XAttribute precisionAttrib = ProfileHelper.FindAttribute(element, true, "precision");
            if (precisionAttrib != null)
            {
                if (!float.TryParse(precisionAttrib.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out precision))
                    throw new ProfileAttributeExpectedException<float>(precisionAttrib);
            }
            else
                precision = 1.5f;

            XAttribute questIdAttrib = ProfileHelper.FindAttribute(element, true, "QuestId");

            uint questId = 0;
            if (questIdAttrib != null)
            {
                if (!uint.TryParse(questIdAttrib.Value, out questId))
                    throw new ProfileAttributeExpectedException<int>(questIdAttrib);
            }

            NavType navType = NavType.Run;
            XAttribute navTypeAttrib = ProfileHelper.FindAttribute(element, true, "Nav", "NavType");
            if (navTypeAttrib != null && !Enum.TryParse<NavType>(navTypeAttrib.Value, out navType))
                throw new ProfileAttributeExpectedException(navTypeAttrib, Enum.GetNames(typeof(NavType)));

            return new MoveToNode(loc, name, precision, questId, navType);
        }
    }
}
