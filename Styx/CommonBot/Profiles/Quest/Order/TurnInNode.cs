using System;
using System.Xml.Linq;
using Styx.Common;

namespace Styx.CommonBot.Profiles.Quest.Order
{
    public class TurnInNode : OrderNode
    {
        public TurnInNode(WoWPoint turninLocation, uint turnInId, string turnInName, QuestObjectType? turnInType, uint questId, string questName, NavType navType) : base(OrderNodeType.TurnIn)
        {
            TurnInId = turnInId;
            TurnInName = turnInName;
            TurnInType = turnInType;
            QuestId = questId;
            QuestName = questName;
            TurnInLocation = turninLocation;
            NavType = navType;
        }

        public WoWPoint TurnInLocation { get; private set; }
        public uint TurnInId { get; private set; }
        public string TurnInName { get; private set; }
        public QuestObjectType? TurnInType { get; private set; }
        public uint QuestId { get; private set; }
        public string QuestName { get; private set; }
        public NavType NavType { get; private set; }

        public override string ToString()
        {
            return string.Format("[TurnInNode TurnInId: {0}, TurnInName: {1}, QuestId: {2}, QuestName: {3}]", TurnInId, Utilities.GetObjectString(TurnInName, "(null)"), QuestId, Utilities.GetObjectString(QuestName, "(null)"));
        }

        public static new TurnInNode FromXml(XElement element)
        {
            WoWPoint turnInLocation;
            try
            {
                turnInLocation = ProfileHelper.ExtractLocationFromElement(element);
            }
            catch (ProfileException)
            {
                turnInLocation = WoWPoint.Zero;
            }

            XAttribute turnInIdAttrib = ProfileHelper.FindAttribute(element, true, "turninid");
            int turnInId = 0;
            if (turnInIdAttrib != null)
            {
                if (!int.TryParse(turnInIdAttrib.Value, out turnInId))
                    throw new ProfileAttributeExpectedException<int>(turnInIdAttrib);
            }

            XAttribute turnInNameAttrib = ProfileHelper.FindAttribute(element, true, "turninname");
            string turnInName = null;
            if (turnInNameAttrib != null)
                turnInName = turnInNameAttrib.Value;

            QuestObjectType? type = null;
            XAttribute turnInTypeAttrib = ProfileHelper.FindAttribute(element, true, "TurnInType");
            if (turnInTypeAttrib != null)
            {
                type = PickUpNode.ParseQuestObjectType(turnInTypeAttrib.Value);
                if (!type.HasValue || type == QuestObjectType.Item)
                    throw new ProfileAttributeExpectedException(turnInTypeAttrib, "Object", "Npc");
            }

            XAttribute questIdAttrib = ProfileHelper.FindAttribute(element, true, "QuestId");
            int questId;
            if (questIdAttrib == null || !int.TryParse(questIdAttrib.Value, out questId))
                questId = 0;

            XAttribute questNameAttrib = ProfileHelper.FindAttribute(element, true, "QuestName");
            string questName = null;
            if (questNameAttrib != null)
                questName = questNameAttrib.Value;

            XAttribute navTypeAttrib = ProfileHelper.FindAttribute(element, true, "Nav", "NavType");
            NavType navType = NavType.Run;
            if (navTypeAttrib != null && !Enum.TryParse<NavType>(navTypeAttrib.Value, out navType))
                throw new ProfileAttributeExpectedException(navTypeAttrib, Enum.GetNames(typeof(NavType)));

            return new TurnInNode(turnInLocation, (uint)turnInId, turnInName, type, (uint)questId, questName, navType);
        }
    }
}
