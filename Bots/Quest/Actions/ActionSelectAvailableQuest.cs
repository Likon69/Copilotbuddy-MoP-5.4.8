using System.Collections.Generic;
using Styx;
using Styx.Common;
using Styx.CommonBot.Frames;
using Styx.TreeSharp;
using Styx.WoWInternals;

namespace Bots.Quest.Actions
{
	public class ActionSelectAvailableQuest : Action
	{
		private readonly int _questId;
        
		public ActionSelectAvailableQuest(int id)
		{
			_questId = id;
		}

		protected override RunStatus Run(object context)
		{
            //if (QuestManager.QuestFrame.IsVisible && QuestManager.QuestFrame.CurrentShownQuestId != 0)
            //    return RunStatus.Success;

            if (QuestManager.GossipFrame.IsVisible)
			{
                var availableQuests = QuestManager.GossipFrame.AvailableQuests;
                for (int i = 0; i < availableQuests.Count; i++)
				{
                    if (_questId == -1 || availableQuests[i].Id == _questId)
					{
                        PlayerQuest quest = StyxWoW.Me.QuestLog.GetQuestById((uint)availableQuests[i].Id);
						if (_questId == -1 && !quest.IsCompleted)
							continue;

                        QuestManager.GossipFrame.SelectAvailableQuest(availableQuests[i].Index);
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

                var availableQuests = QuestManager.QuestFrame.AvailableQuests;
                for (int i = 0; i < availableQuests.Count; i++)
                {
                    if (_questId == -1 || availableQuests[i].Id == _questId)
                    {
                        PlayerQuest quest = StyxWoW.Me.QuestLog.GetQuestById((uint)availableQuests[i].Id);
                        if (_questId == -1 && !quest.IsCompleted)
                            continue;

                        QuestManager.QuestFrame.SelectAvailableQuest(availableQuests[i].Index);
                        return RunStatus.Success;
                    }
                }
            }

			return RunStatus.Failure;
		}
	}
}
