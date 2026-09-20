using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using GreenMagic;
using Styx.Common;
using Styx.Patchables;
using Styx.Offsets;

namespace Styx.WoWInternals
{
    public class QuestLog
    {
        public static QuestLog Instance
        {
            get
            {
                QuestLog questLog;
                if ((questLog = _instance) == null)
                {
                    questLog = (_instance = new QuestLog());
                }
                return questLog;
            }
        }

        private QuestLog()
        {
            Lua.Events.AttachEvent("QUEST_AUTOCOMPLETE", new LuaEventHandlerDelegate(OnQuestCompleted));
            Lua.Events.AttachEvent("QUEST_FINISHED", new LuaEventHandlerDelegate(OnQuestCompleted));
        }

        private void OnQuestCompleted(object sender, LuaEventArgs e)
        {
            _completedQuestsStopwatch.Reset();
        }

        private static string CompletedQuestsCacheFilePath
        {
            get { return Path.Combine(Utilities.AssemblyDirectory, "Cache", string.Format("{0}.cqc", StyxWoW.Me.Guid.ToString("X"))); }
        }

        public ReadOnlyCollection<uint> GetCompletedQuests()
        {
            if (!_completedQuestsStopwatch.IsRunning || _completedQuestsStopwatch.ElapsedMilliseconds > 60000L)
            {
                foreach (uint num in GetQuestsCompleted())
                {
                    AddCompletedQuest(num, false);
                }
                using (StyxWoW.Memory.ReleaseFrame(true))
                {
                    LastCompletedQuests.SaveFile();
                }
                _completedQuestsStopwatch.Restart();
            }
            return LastCompletedQuests.AsReadOnly();
        }

        internal static IEnumerable<uint> GetQuestsCompleted()
        {
            IntPtr questBitPtr = StyxWoW.Offsets.Statics.QuestsFlaggedCompletedList;
            WoWDb.DbTable questDb = StyxWoW.Db[ClientDb.QuestV2];
            foreach (WoWDb.Row row in questDb)
            {
                int index = row.GetField<int>(1);
                uint id = row.GetField<uint>(0);
                if (id != 0 && index > 0)
                {
                    byte b = StyxWoW.Memory.Read<byte>(questBitPtr + ((index - 1) >> 3));
                    if (((1 << ((index - 1) & 7)) & b) != 0)
                    {
                        yield return id;
                    }
                }
            }
        }

        internal void AddCompletedQuest(uint questId, bool save)
        {
            if (LastCompletedQuests.Contains(questId))
            {
                return;
            }
            try
            {
                Quest quest = Quest.FromId(questId);
                LastCompletedQuests.Add(questId, quest == null || quest.IsDaily || quest.IsWeekly);
                if (save)
                {
                    LastCompletedQuests.SaveFile();
                }
            }
            catch (Exception ex)
            {
                Logging.WriteException(ex);
            }
        }

        public uint QuestCount
        {
            get { return StyxWoW.Memory.Read<uint>(StyxWoW.Offsets.Statics.NumQuestsInQuestFrame); }
        }

        public int GetIndexForQuest(uint questId)
        {
            WoWDescriptorQuest[] array = GetDescriptorsQuestLog();
            for (int i = 0; i < 50; i++)
            {
                uint id = array[i].Id;
                if (id == questId)
                {
                    return i;
                }
            }
            return -1;
        }

        public List<PlayerQuest> GetAllQuests()
        {
            List<PlayerQuest> list = new List<PlayerQuest>();
            WoWDescriptorQuest[] array = GetDescriptorsQuestLog();
            for (int i = 0; i < 50; i++)
            {
                uint id = array[i].Id;
                PlayerQuest playerQuest = PlayerQuest.InternalFromId(id);
                if (playerQuest != null)
                {
                    list.Add(playerQuest);
                }
            }
            return list;
        }

        public PlayerQuest GetQuest(uint index)
        {
            if (index >= 50U)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            uint num = InternalGetQuestId(index);
            if (num <= 0U)
            {
                return null;
            }
            return PlayerQuest.InternalFromId(num);
        }

        public uint GetQuestId(uint index)
        {
            if (index >= 50U)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            return InternalGetQuestId(index);
        }

        internal uint InternalGetQuestId(uint index)
        {
            return GetDescriptorsQuestLog()[(int)((UIntPtr)index)].Id;
        }

        internal WoWDescriptorQuest[] GetDescriptorsQuestLog()
        {
            return StyxWoW.Me.GetStorageFieldArray<WoWDescriptorQuest>((uint)WoWPlayerFields.QuestLog, 50);
        }

        public bool ContainsQuest(uint questId)
        {
            if (questId == 0U)
            {
                return false;
            }
            WoWDescriptorQuest[] array = GetDescriptorsQuestLog();
            for (int i = 0; i < 50; i++)
            {
                WoWDescriptorQuest woWDescriptorQuest = array[i];
                if (woWDescriptorQuest.Id == questId)
                {
                    return true;
                }
            }
            return false;
        }

        public PlayerQuest GetQuestById(uint questID)
        {
            if (!ContainsQuest(questID))
            {
                return null;
            }
            return PlayerQuest.InternalFromId(questID);
        }

        public WoWDescriptorQuest GetQuestInfo(int index)
        {
            if (index < 0 || index >= 50)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            return GetDescriptorsQuestLog()[index];
        }

        public void AbandonQuestById(uint questId)
        {
            Executor executor = StyxWoW.Memory.Executor;
            if (executor == null)
            {
                throw new Exception("Invalid executor in AbandonQuestById");
            }
            lock (executor.AssemblyLock)
            {
                executor.Clear();
                executor.AddRandomLine("mov esi, {0}", new object[] { StyxWoW.Offsets.Statics.AbandoningQuest });
                executor.AddRandomLine("mov ebx, [esi]", new object[0]);
                executor.AddRandomLine("mov eax, {0}", new object[] { questId });
                executor.AddRandomLine("mov [esi], eax", new object[0]);
                executor.AddRandomLine("call {0}", new object[] { StyxWoW.Offsets.Functions.CGQuestLog__AbandonSelectedQuest });
                executor.AddRandomLine("mov [esi], ebx", new object[0]);
                executor.AddRandomLine("retn", new object[0]);
                executor.Execute();
            }
        }

        public const int MaxQuestsInLog = 50;

        private static QuestLog _instance;

        private static readonly FileCache<uint> LastCompletedQuests = new FileCache<uint>(CompletedQuestsCacheFilePath, true);

        private static readonly Stopwatch _completedQuestsStopwatch = new Stopwatch();
    }
}
