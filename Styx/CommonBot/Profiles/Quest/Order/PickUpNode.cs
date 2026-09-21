using System;
using System.Xml.Linq;
using Styx.Common;

namespace Styx.CommonBot.Profiles.Quest.Order
{
    /// <summary>
    /// A pick up tells HB to pick up a quest.
    /// </summary>
    public class PickUpNode : OrderNode
    {
        public PickUpNode(WoWPoint giverLocation, uint giverId, string giverName, QuestObjectType? giverType, uint questId, string questName, NavType navType) : base(OrderNodeType.PickUp)
        {
            GiverLocation = giverLocation;
            GiverId = giverId;
            GiverName = giverName;
            GiverType = giverType;
            QuestId = questId;
            QuestName = questName;
            NavType = navType;
        }

        public WoWPoint GiverLocation { get; private set; }
        public uint GiverId { get; private set; }
        public string GiverName { get; private set; }
        public QuestObjectType? GiverType { get; private set; }
        public uint QuestId { get; private set; }
        public string QuestName { get; private set; }
        public NavType NavType { get; private set; }

        public override string ToString()
        {
            return string.Format("[PickUpNode GiverLocation: {0}, GiverId: {1}, GiverName: {2}, GiverType: {3}, QuestId: {4}, QuestName: {5}]", GiverLocation, GiverId, Utilities.GetObjectString(GiverName, "(null)"), Utilities.GetObjectString(GiverType, "(null)"), QuestId, Utilities.GetObjectString(QuestName, "(null)"));
        }

        public static new PickUpNode FromXml(XElement element)
        {
            WoWPoint giverLocation;
            try
            {
                giverLocation = ProfileHelper.ExtractLocationFromElement(element);
            }
            catch (ProfileException)
            {
                giverLocation = WoWPoint.Zero;
            }

            XAttribute giverIdAttrib = ProfileHelper.FindAttribute(element, true, "GiverId");
            int giverId = 0;
            if (giverIdAttrib != null)
            {
                if (!int.TryParse(giverIdAttrib.Value, out giverId))
                    throw new ProfileAttributeExpectedException<int>(giverIdAttrib);
            }

            XAttribute giverNameAttrib = ProfileHelper.FindAttribute(element, true, "GiverName");
            string giverName = null;
            if (giverNameAttrib != null)
                giverName = giverNameAttrib.Value;

            if (giverId == 0 && giverName == null)
                throw new ProfileMissingAttributeException<int>("GiverId", element);

            QuestObjectType? type = null;
            XAttribute giverTypeAttrib = ProfileHelper.FindAttribute(element, true, "GiverType");
            if (giverTypeAttrib != null)
            {
                type = ParseQuestObjectType(giverTypeAttrib.Value);
                if (!type.HasValue)
                    throw new ProfileAttributeExpectedException(giverTypeAttrib, "Object", "Npc", "Item");
            }

            XAttribute questIdAttrib = ProfileHelper.FindAttribute(element, true, "QuestId");
            if (questIdAttrib == null)
                throw new ProfileMissingAttributeException<int>("QuestId", element);

            int questId;
            if (!int.TryParse(questIdAttrib.Value, out questId))
                throw new ProfileAttributeExpectedException<int>(questIdAttrib);

            XAttribute questNameAttrib = ProfileHelper.FindAttribute(element, true, "QuestName");
            string questName = null;
            if (questNameAttrib != null)
                questName = questNameAttrib.Value;

            XAttribute navTypeAttrib = ProfileHelper.FindAttribute(element, true, "Nav", "NavType");
            NavType navType = NavType.Run;
            if (navTypeAttrib != null && !Enum.TryParse<NavType>(navTypeAttrib.Value, out navType))
                throw new ProfileAttributeExpectedException(navTypeAttrib, Enum.GetNames(typeof(NavType)));

            return new PickUpNode(giverLocation, (uint)giverId, giverName, type, (uint)questId, questName, navType);
        }

        internal static QuestObjectType? ParseQuestObjectType(string typeString)
        {
            switch (typeString.ToLowerInvariant())
            {
                case "obj":
                case "object":
                case "gameobj":
                case "gameobject":
                    return QuestObjectType.GameObject;
                case "npc":
                case "unit":
                    return QuestObjectType.Npc;
                case "item":
                    return QuestObjectType.Item;
                default:
                    return null;
            }
        }
    }
}
