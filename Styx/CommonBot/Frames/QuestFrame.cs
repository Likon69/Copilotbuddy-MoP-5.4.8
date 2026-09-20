using System;
using System.Collections.Generic;
using Styx.WoWInternals;

namespace Styx.CommonBot.Frames
{
    public class QuestFrame : Frame
    {
        public static readonly QuestFrame Instance = new QuestFrame();

        public QuestFrame()
            : base("QuestFrame")
        {
        }

        public void AcceptQuest()
        {
            Lua.DoString("AcceptQuest() if QuestFrameAcceptButton:IsVisible() then QuestFrameAcceptButton:Click() end", "WoW.lua");
        }

        public void CompleteQuest()
        {
            Lua.DoString("CompleteQuest() if QuestFrameCompleteQuestButton:IsVisible() or QuestFrameCompleteButton:IsVisible() then QuestFrameCompleteQuestButton:Click() QuestFrameCompleteButton:Click() end QuestFrameCompleteQuestButton:Click() QuestFrameCompleteButton:Click()", "WoW.lua");
        }

        public void ClickContinue()
        {
            Lua.DoString("QuestFrameCompleteButton:Click()", "WoW.lua");
        }

        public void DeclineQuest()
        {
            Lua.DoString("DeclineQuest()", "WoW.lua");
        }

        public void SelectQuestReward(int index)
        {
            Lua.DoString("QuestInfoItem{0}:Click()", index + 1);
        }

        public new void Hide()
        {
            Close();
        }

        public void Close()
        {
            Lua.DoString("CloseQuest() DeclineQuest() QuestFrameCloseButton:Click()", "WoW.lua");
        }

        public uint CurrentShownQuestId
        {
            get
            {
                return StyxWoW.Memory.Read<uint>(StyxWoW.Offsets.Statics.CurrentShownQuest);
            }
        }

        public Quest CurrentShownQuest
        {
            get
            {
                return Quest.FromId(CurrentShownQuestId);
            }
        }

        public List<GossipQuestEntry> ActiveQuests
        {
            get
            {
                if (IsVisible)
                {
                    var quests = new List<GossipQuestEntry>();

                    for (int i = 0; i < 40; i++)
                    {
                        try
                        {
                            var quest = GetActiveQuestInfoFromIndex(i);

                            if (quest != null)
                                quests.Add(quest);
                        }
                        catch
                        {
                            break;
                        }
                    }

                    return quests;
                }

                return null;
            }
        }

        public List<GossipQuestEntry> AvailableQuests
        {
            get
            {
                var quests = new List<GossipQuestEntry>();

                using (StyxWoW.Memory.AcquireFrame())
                {
                    if (IsVisible)
                    {
                        for (int i = 0; i < 40; i++)
                        {
                            try
                            {
                                var quest = GetAvaibleQuestInfoFromIndex(i);
                                if (quest != null)
                                    quests.Add(quest);
                            }
                            catch
                            {
                                break;
                            }
                        }
                    }
                }

                return quests;
            }
        }

        private static GossipQuestEntry GetAvaibleQuestInfoFromIndex(int index)
        {
            return GossipFrame.GetAvaibleQuestInfoFromIndex(index);
        }

        private static GossipQuestEntry GetActiveQuestInfoFromIndex(int index)
        {
            return GossipFrame.GetActiveQuestInfoFromIndex(index);
        }

        public void SelectActiveGossipQuest(int index)
        {
            Lua.DoString(string.Format("SelectGossipActiveQuest({0})", index + 1), "WoW.lua");
        }

        public void SelectActiveQuest(int index)
        {
            Lua.DoString(string.Format("SelectActiveQuest({0})", index + 1), "WoW.lua");
        }

        public void SelectAvailableQuest(int index)
        {
            Lua.DoString(string.Format("SelectAvailableQuest({0}) SelectGossipAvailableQuest({0})", index + 1), "_main.lua");
        }
    }
}
