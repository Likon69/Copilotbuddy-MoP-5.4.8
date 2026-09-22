using System.Collections.Generic;
using Styx;
using Styx.Common;
using Styx.CommonBot.Frames;
using Styx.TreeSharp;
using Styx.WoWInternals;

namespace Bots.Quest.Actions
{
	public class ActionSelectActiveQuest : Action
	{
		private readonly int _questId;
        
		public ActionSelectActiveQuest(int id)
		{
			_questId = id;
		}

		protected override RunStatus Run(object context)
		{
            //if (QuestManager.QuestFrame.IsVisible && QuestManager.QuestFrame.CurrentShownQuestId != 0)
            //    return RunStatus.Success;

            if (QuestManager.GossipFrame.IsVisible)
			{
                var activeQuests = QuestManager.GossipFrame.ActiveQuests;
				for (int i = 0; i < activeQuests.Count; i++)
				{
					if (_questId == -1 || activeQuests[i].Id == _questId)
					{
						PlayerQuest quest = StyxWoW.Me.QuestLog.GetQuestById((uint)activeQuests[i].Id);
						if (_questId == -1 && !quest.IsCompleted)
							continue;

                        QuestManager.GossipFrame.SelectActiveGossipQuest(activeQuests[i].Index);
						return RunStatus.Success;
					}
				}
			}

            if (QuestManager.QuestFrame.IsVisible)
            {
                if (QuestFrame.Instance.CurrentShownQuestId == _questId)
                    return RunStatus.Failure;

				if (QuestFrame.Instance.CurrentShownQuestId != 0 && QuestFrame.Instance.CurrentShownQuestId != _questId)
				{
					Logging.WriteDiagnostic("Current shown quest is not the quest we want. Closing the quest frame to select to correct quest.");
					QuestFrame.Instance.Close();
					return RunStatus.Success;
				}

                var activeQuests = QuestManager.QuestFrame.ActiveQuests;
                for (int i = 0; i < activeQuests.Count; i++)
                {
                    if (_questId == -1 || activeQuests[i].Id == _questId)
                    {
                        PlayerQuest quest = StyxWoW.Me.QuestLog.GetQuestById((uint)activeQuests[i].Id);
                        if (_questId == -1 && !quest.IsCompleted)
                            continue;

                        QuestManager.QuestFrame.SelectActiveQuest(activeQuests[i].Index);
                        return RunStatus.Success;
                    }
                }
            }

			return RunStatus.Failure;
		}
	}
}
