using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Styx.Resources;

namespace Styx.CommonBot.Profiles.Quest
{
    public class ObjectiveInfo
    {
        public ObjectiveInfo(ObjectiveType type)
        {
            Type = type;
        }

        public ObjectiveType Type { get; private set; }

        public static ObjectiveInfo FromXML(XElement element)
        {
            switch (element.Name.ToString().ToLowerInvariant())
            {
                case "turnin":
                case "handin":
                    return _objectiveParser[ObjectiveType.TurnIn](element);
                case "objective":
                    XAttribute typeAttrib = ProfileHelper.FindAttribute(element, true, "type");
                    if (typeAttrib == null)
                        throw new ProfileException(StyxResources.Objective_info_is_missing_type_);

                    ObjectiveType type;
                    if (!ParseQuestObjectiveType(typeAttrib.Value, out type))
                        throw new ProfileException(StyxResources.Expected_objective_type_);

                    typeAttrib.Remove();

                    return _objectiveParser[type](element);
                default:
                    throw new ProfileUnknownElementException(element);
            }
        }

        internal static bool ParseQuestObjectiveType(string value, out ObjectiveType type)
        {
            switch (value.ToLowerInvariant())
            {
                case "turnin":
                case "handin":
                    type = ObjectiveType.TurnIn;
                    return true;
                case "grind":
                case "kill":
                case "killmob":
                case "slay":
                case "slaymob":
                    type = ObjectiveType.KillMob;
                    return true;
                case "gather":
                case "getitem":
                case "gatheritem":
                case "collect":
                case "collectitem":
                    type = ObjectiveType.CollectItem;
                    return true;
                case "useobject":
                case "use":
                    type = ObjectiveType.UseObject;
                    return true;
                default:
                    type = ObjectiveType.None;
                    return false;
            }
        }

        private static readonly Dictionary<ObjectiveType, Func<XElement, ObjectiveInfo>> _objectiveParser = new Dictionary<ObjectiveType, Func<XElement, ObjectiveInfo>>
        {
            { ObjectiveType.TurnIn, TurnInObjectiveInfo.FromXML },
            { ObjectiveType.KillMob, KillMobObjectiveInfo.FromXML },
            { ObjectiveType.CollectItem, CollectItemObjectiveInfo.FromXML },
            { ObjectiveType.UseObject, UseObjectObjectiveInfo.FromXML },
        };
    }
}
