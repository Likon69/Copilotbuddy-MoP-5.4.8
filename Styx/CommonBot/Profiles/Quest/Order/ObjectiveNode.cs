using System;
using System.Xml.Linq;
using Styx.Common;

namespace Styx.CommonBot.Profiles.Quest.Order
{
    /// <summary>
    /// A quest objective tells HB to perform an objective for a quest.
    /// </summary>
    public class ObjectiveNode : OrderNode
    {
        public ObjectiveNode(uint questId, string questName, ObjectiveType objectiveType, uint objectiveId, string objectiveName, uint objectiveCount, WoWPoint forceHotspot, NavType navType) : base(OrderNodeType.Objective)
        {
            QuestId = questId;
            QuestName = questName;
            ObjectiveType = objectiveType;
            ObjectiveId = objectiveId;
            ObjectiveName = objectiveName;
            ObjectiveCount = objectiveCount;
            ForcedHotspot = forceHotspot;
            NavType = navType;
        }

        public uint QuestId { get; private set; }
        public string QuestName { get; private set; }
        public ObjectiveType ObjectiveType { get; private set; }
        public uint ObjectiveId { get; private set; }
        public string ObjectiveName { get; private set; }
        public uint ObjectiveCount { get; private set; }
        public WoWPoint ForcedHotspot { get; private set; }
        public NavType NavType { get; private set; }

        public override string ToString()
        {
            return string.Format("[ObjectiveNode QuestId: {0}, QuestName: {1}, ObjectiveType: {2}, ObjectiveId: {3}, ObjectiveName: {4}, ObjectiveCount: {5}]", QuestId, Utilities.GetObjectString(QuestName, "(null)"), ObjectiveType, ObjectiveId, Utilities.GetObjectString(ObjectiveName, "(null)"), ObjectiveCount);
        }

        public static new ObjectiveNode FromXml(XElement element)
        {
            WoWPoint forcedHotspot = ProfileHelper.ExtractLocationFromElement(element);

            XAttribute questIdAttrib = ProfileHelper.FindAttribute(element, true, "quest", "questid");
            if (questIdAttrib == null)
                throw new ProfileMissingAttributeException<int>("QuestId", element);

            int questId;
            if (!int.TryParse(questIdAttrib.Value, out questId))
                throw new ProfileAttributeExpectedException<int>(questIdAttrib);

            XAttribute questNameAttrib = ProfileHelper.FindAttribute(element, true, "QuestName");
            string questName = null;
            if (questNameAttrib != null)
                questName = questNameAttrib.Value;

            XAttribute objectiveTypeAttrib = ProfileHelper.FindAttribute(element, true, "type");
            if (objectiveTypeAttrib == null)
                throw new ProfileMissingAttributeException("Type", element);

            ObjectiveType type;
            if (ObjectiveInfo.ParseQuestObjectiveType(objectiveTypeAttrib.Value, out type))
            {
                if (type == ObjectiveType.CollectItem || type == ObjectiveType.KillMob || type == ObjectiveType.UseObject)
                {
                    string[] idNames = GetIdNames(type);
                    XAttribute objectiveIdAttrib = ProfileHelper.FindAttribute(element, true, idNames);
                    if (objectiveIdAttrib == null)
                        throw new ProfileMissingAttributeException<int>(GetIdName(type), element);

                    int objectiveId;
                    if (!int.TryParse(objectiveIdAttrib.Value, out objectiveId))
                        throw new ProfileAttributeExpectedException<int>(objectiveIdAttrib);

                    string objectiveName = null;
                    XAttribute objectiveNameAttrib = ProfileHelper.FindAttribute(element, true, GetNameNames(type));
                    if (objectiveNameAttrib != null)
                        objectiveName = objectiveNameAttrib.Value;

                    int objectiveCount = -1;
                    XAttribute objectiveCountAttrib = ProfileHelper.FindAttribute(element, true, GetCountNames(type));
                    if (objectiveCountAttrib != null && !int.TryParse(objectiveCountAttrib.Value, out objectiveCount))
                        throw new ProfileAttributeExpectedException<int>(objectiveCountAttrib);

                    XAttribute navTypeAttrib = ProfileHelper.FindAttribute(element, true, "Nav", "NavType");
                    NavType navType = NavType.Run;
                    if (navTypeAttrib != null && !Enum.TryParse<NavType>(navTypeAttrib.Value, out navType))
                        throw new ProfileAttributeExpectedException(navTypeAttrib, Enum.GetNames(typeof(NavType)));

                    return new ObjectiveNode((uint)questId, questName, type, (uint)objectiveId, objectiveName, (uint)objectiveCount, forcedHotspot, navType);
                }
            }

            throw new ProfileAttributeExpectedException(objectiveTypeAttrib);
        }

        private static string GetIdName(ObjectiveType type)
        {
            switch (type)
            {
                case ObjectiveType.KillMob:
                    return "mobid";
                case ObjectiveType.CollectItem:
                    return "itemid";
                case ObjectiveType.UseObject:
                    return "objectid";
                default:
                    return null;
            }
        }

        private static string[] GetIdNames(ObjectiveType type)
        {
            switch (type)
            {
                case ObjectiveType.KillMob:
                    return new[] { "id", "entry", "mobid", "mobentry" };
                case ObjectiveType.CollectItem:
                    return new[] { "id", "entry", "itemid", "itementry" };
                case ObjectiveType.UseObject:
                    return new[] { "id", "entry", "gameobjectid", "objectid", "gameobjectentry", "objectentry", "usegameobject", "useobject" };
                default:
                    return null;
            }
        }

        private static string[] GetNameNames(ObjectiveType type)
        {
            switch (type)
            {
                case ObjectiveType.KillMob:
                    return new[] { "name", "mobname" };
                case ObjectiveType.CollectItem:
                    return new[] { "name", "itemname" };
                case ObjectiveType.UseObject:
                    return new[] { "name", "objectname" };
                default:
                    return null;
            }
        }

        private static string[] GetCountNames(ObjectiveType type)
        {
            switch (type)
            {
                case ObjectiveType.KillMob:
                    return new[] { "count", "killcount", "slaycount" };
                case ObjectiveType.CollectItem:
                    return new[] { "count", "collectcount" };
                case ObjectiveType.UseObject:
                    return new[] { "count", "gameobjectcount", "objectcount", "usecount" };
                default:
                    return null;
            }
        }
    }
}
