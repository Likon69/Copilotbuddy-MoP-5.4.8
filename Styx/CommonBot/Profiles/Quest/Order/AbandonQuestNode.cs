using System.Xml.Linq;

namespace Styx.CommonBot.Profiles.Quest.Order
{
    public class AbandonQuestNode : OrderNode
    {
        public AbandonQuestNode(uint questId) : base(OrderNodeType.AbandonQuest)
        {
            QuestId = questId;
        }

        public uint QuestId { get; private set; }

        public static new AbandonQuestNode FromXml(XElement element)
        {
            XAttribute questIdAttrib = ProfileHelper.FindAttribute(element, true, "QuestId", "Id");
            if (questIdAttrib == null)
                throw new ProfileMissingAttributeException<int>("QuestId", element);

            int questId;
            if (!int.TryParse(questIdAttrib.Value, out questId))
                throw new ProfileAttributeExpectedException<int>(questIdAttrib);

            return new AbandonQuestNode((uint)questId);
        }

        public override string ToString()
        {
            return string.Format("[AbandonQuestNode QuestId: {0}]", QuestId);
        }
    }
}
