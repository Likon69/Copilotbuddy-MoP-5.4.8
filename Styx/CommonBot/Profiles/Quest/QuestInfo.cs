using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Styx.Common;

namespace Styx.CommonBot.Profiles.Quest
{
    public class QuestInfo
    {
        public QuestInfo(uint id, string name)
        {
            Objectives = new List<ObjectiveInfo>();
            ID = id;
            Name = name;
        }

        public uint ID { get; private set; }
        public string Name { get; private set; }
        public List<ObjectiveInfo> Objectives { get; private set; }

        public static QuestInfo FromXML(XElement element)
        {
            uint id = 0;
            string name = "";
            foreach (XAttribute attrib in element.Attributes())
            {
                try
                {
                    switch (attrib.Name.ToString().ToLowerInvariant())
                    {
                        case "entry":
                        case "id":
                            if (!uint.TryParse(attrib.Value, out id))
                            {
                                throw new ProfileAttributeExpectedException<uint>(attrib);
                            }
                            break;
                        case "name":
                            name = attrib.Value;
                            break;
                    }
                }
                catch (ProfileException ex)
                {
                    Logging.WriteDiagnostic(ex.Message);
                }
            }

            if (id == 0)
            {
                throw new ProfileMissingAttributeException<uint>("Id", element);
            }

            var info = new QuestInfo(id, name);
            foreach (XElement subElement in element.Elements())
            {
                try
                {
                    switch (subElement.Name.ToString().ToLowerInvariant())
                    {
                        case "objective":
                        case "turnin":
                        case "handin":
                            info.Objectives.Add(ObjectiveInfo.FromXML(subElement));
                            break;
                    }
                }
                catch (ProfileException ex)
                {
                    Logging.WriteDiagnostic(ex.Message);
                }
            }

            return info;
        }

        public TurnInObjectiveInfo FindTurnIn()
        {
            return (TurnInObjectiveInfo)Objectives.FirstOrDefault(obj => obj is TurnInObjectiveInfo && obj.Type == ObjectiveType.TurnIn);
        }

        public KillMobObjectiveInfo FindKillMob(uint mobId)
        {
            return (KillMobObjectiveInfo)Objectives.FirstOrDefault(obj => obj is KillMobObjectiveInfo && obj.Type == ObjectiveType.KillMob && ((KillMobObjectiveInfo)obj).MobID == mobId);
        }

        public CollectItemObjectiveInfo FindCollectItem(uint itemID)
        {
            return (CollectItemObjectiveInfo)Objectives.FirstOrDefault(obj => obj is CollectItemObjectiveInfo && obj.Type == ObjectiveType.CollectItem && ((CollectItemObjectiveInfo)obj).ItemID == itemID);
        }

        public UseObjectObjectiveInfo FindUseGameObject(uint gameObjectID)
        {
            return (UseObjectObjectiveInfo)Objectives.FirstOrDefault(obj => obj is UseObjectObjectiveInfo && obj.Type == ObjectiveType.UseObject && ((UseObjectObjectiveInfo)obj).ObjectID == gameObjectID);
        }
    }
}
