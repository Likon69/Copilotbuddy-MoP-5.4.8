using System;
using GreenMagic;
using Styx.Common;
using Styx.WoWInternals.WoWCache;

namespace Styx.WoWInternals
{
    public class PlayerQuest : Quest
    {
        protected PlayerQuest(Styx.WoWInternals.WoWCache.WoWCache.QuestCacheEntry entry)
            : base(entry)
        {
        }

        public bool IsCompleted
        {
            get
            {
                return IsQuestCompleted();
            }
        }

        public bool IsFailed
        {
            get
            {
                return IsQuestFailed();
            }
        }

        public WoWQuestPOIInfo GetQuestPOIInfo()
        {
            WoWQuestPOIInfo info;
            GetQuestPOIInfo(out info);
            return info;
        }

        private bool IsQuestCompleted()
        {
            WoWDescriptorQuest data;
            return (GetData(out data) && (data.Flags & WoWDescriptorQuestFlags.Completed) != (WoWDescriptorQuestFlags)0) || IsQuestCompletedInjection();
        }

        private bool IsQuestFailed()
        {
            WoWDescriptorQuest data;
            return GetData(out data) && (data.Flags & WoWDescriptorQuestFlags.Failed) != (WoWDescriptorQuestFlags)0;
        }

        public bool GetQuestPOIInfo(out WoWQuestPOIInfo info)
        {
            WoWQuestPOIInfo[] array = StyxWoW.Memory.ReadArray<WoWQuestPOIInfo>(StyxWoW.Offsets.Statics.PlayerCurrentQuests, 50);
            for (int i = 0; i < 50; i++)
            {
                int questID = array[i].QuestID;
                if (questID <= 0)
                {
                    break;
                }
                if ((long)questID == (long)((ulong)Id))
                {
                    info = array[i];
                    return true;
                }
            }
            info = default(WoWQuestPOIInfo);
            return false;
        }

        private int GetDescriptorIndex(WoWDescriptorQuest[] quests)
        {
            for (int i = 0; i < quests.Length; i++)
            {
                if (quests[i].Id == Id)
                {
                    return i;
                }
            }
            return -1;
        }

        public bool GetData(out WoWDescriptorQuest data)
        {
            WoWDescriptorQuest[] array = StyxWoW.Me.QuestLog.GetDescriptorsQuestLog();
            int num = GetDescriptorIndex(array);
            if (num >= 0 && num < array.Length)
            {
                data = array[num];
                return true;
            }
            data = default(WoWDescriptorQuest);
            return false;
        }

        private bool IsQuestCompletedInjection()
        {
            Guard.AgainstInvalidExecutor();
            Executor executor = StyxWoW.Memory.Executor;
            bool flag2;
            lock (executor.AssemblyLock)
            {
                try
                {
                    executor.Clear();
                    executor.AddLine("push {0}", new object[] { Id });
                    executor.AddLine("call {0}", new object[] { StyxWoW.Offsets.Functions.CGQuestLog__GetLuaQuestIndexByID });
                    executor.AddLine("add esp, 4", new object[0]);
                    executor.AddLine("mov esi, eax", new object[0]);
                    executor.AddLine("sub esi, 1", new object[0]);
                    executor.AddLine("push 1", new object[0]);
                    executor.AddLine("push esi", new object[0]);
                    executor.AddLine("call {0}", new object[] { StyxWoW.Offsets.Functions.CGQuestLog__IsQuestCompleted });
                    executor.AddLine("add esp, 8", new object[0]);
                    executor.AddLine("test eax, eax", new object[0]);
                    executor.AddLine("jnz @IsCompleted", new object[0]);
                    executor.AddLine("push esi", new object[0]);
                    executor.AddLine("call {0}", new object[] { StyxWoW.Offsets.Functions.CGQuestLog__IsQuestCompleted_2 });
                    executor.AddLine("add esp, 4", new object[0]);
                    executor.AddLine("test eax, eax", new object[0]);
                    executor.AddLine("jz @IsNotCompleted", new object[0]);
                    executor.AddLine("@IsCompleted:", new object[0]);
                    executor.AddLine("mov eax, 1", new object[0]);
                    executor.AddLine("retn", new object[0]);
                    executor.AddLine("@IsNotCompleted:", new object[0]);
                    executor.AddLine("mov eax, 0", new object[0]);
                    executor.AddLine("retn", new object[0]);
                    executor.Execute();
                    using (StyxWoW.Memory.TemporaryCacheState(false))
                    {
                        return executor.Memory.Read<bool>(executor.ReturnPointer);
                    }
                }
                catch (Exception ex)
                {
                    Styx.Common.Logging.WriteDiagnostic("Exception in CGQuestLog__IsQuestCompleted:{0} - {1} - {2}", new object[] { ex.Message, ex.StackTrace, ex.Source });
                }
                flag2 = false;
            }
            return flag2;
        }

        internal static PlayerQuest InternalFromId(uint id)
        {
            Styx.WoWInternals.WoWCache.WoWCache.InfoBlock infoBlockById = StyxWoW.Cache[Styx.WoWInternals.WoWCache.CacheDb.Quest].GetInfoBlockById(id);
            if (infoBlockById != null)
            {
                return new PlayerQuest(infoBlockById.Quest);
            }
            return null;
        }

        private const int MaxPOIs = 50;
    }
}
