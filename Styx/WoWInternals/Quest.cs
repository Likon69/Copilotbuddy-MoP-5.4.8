using System;
using System.Collections.Generic;
using System.Linq;
using Styx.Helpers;
using Styx.Patchables;
using Styx.WoWInternals.WoWCache;

namespace Styx.WoWInternals
{
    public class Quest
    {
        internal Quest(Styx.WoWInternals.WoWCache.WoWCache.QuestCacheEntry entry)
        {
            InternalInfo = entry;
            _name = entry.Name.ToUTF8String();
            _description = entry.Description.ToUTF8String();
            _subDescription = entry.EndText.ToUTF8String();
            _completionText = entry.CompletionText.ToUTF8String();
            _objectiveText = entry.ObjectiveText.ToUTF8String();
            _objectives = new string[4];
            if (entry.QuestRewardChoiceCollection != IntPtr.Zero)
            {
                int num = StyxWoW.Memory.Read<int>(entry.QuestRewardChoiceCollection);
                _questRewardChoices = StyxWoW.Memory.ReadArray<Styx.WoWInternals.WoWCache.WoWCache.QuestRewardChoice>(StyxWoW.Memory.Read<IntPtr>(entry.QuestRewardChoiceCollection + 4), num);
                return;
            }

            IEnumerable<Styx.WoWInternals.WoWCache.WoWCache.QuestRewardChoice> questRewardChoices = entry.QuestRewardChoices;
            _questRewardChoices = questRewardChoices.Where(choice => choice.ItemId != 0U).ToArray();
        }

        public uint Id
        {
            get { return InternalInfo.Id; }
        }

        public Styx.WoWInternals.WoWCache.WoWCache.QuestCacheEntry InternalInfo { get; private set; }

        public string Name
        {
            get { return _name; }
        }

        public string Description
        {
            get { return _description; }
        }

        public string SubDescription
        {
            get { return _subDescription; }
        }

        public string CompletionText
        {
            get { return _completionText; }
        }

        public string ObjectiveText
        {
            get { return _objectiveText; }
        }

        public string[] Objectives
        {
            get { return _objectives; }
        }

        public uint[] NormalObjectiveRequiredCounts
        {
            get { return InternalInfo.ObjectiveRequiredCount; }
        }

        public int Level
        {
            get { return (int)InternalInfo.Level; }
        }

        public int RequiredLevel
        {
            get { return (int)InternalInfo.RequiredLevel; }
        }

        public uint RewardMoney
        {
            get { return InternalInfo.RewardMoney; }
        }

        public uint RewardMoneyAtMaxLevel
        {
            get { return InternalInfo.RewardMoneyInsteadOfXP; }
        }

        public WoWSpell RewardSpell
        {
            get
            {
                if (_rewardSpell == null)
                {
                    _rewardSpell = WoWSpell.FromId(RewardSpellId);
                }

                return _rewardSpell;
            }
        }

        public int RewardSpellId
        {
            get { return (int)InternalInfo.RewardSpellId; }
        }

        public uint RewardNumTalentPoints
        {
            get { return InternalInfo.RewardTalentPoints; }
        }

        public uint RewardTitleId
        {
            get { return InternalInfo.RewardTitleId; }
        }

        public int RewardXp
        {
            get { return CalculateRewardXp(); }
        }

        public uint SuggestedPlayers
        {
            get { return InternalInfo.SuggestedPlayers; }
        }

        public bool IsStayAliveQuest
        {
            get { return HasFlag(Styx.WoWInternals.WoWCache.WoWCache.QuestFlags.StayAlive); }
        }

        public bool IsPartyQuest
        {
            get { return HasFlag(Styx.WoWInternals.WoWCache.WoWCache.QuestFlags.PartyQuest); }
        }

        public bool IsShareable
        {
            get { return HasFlag(Styx.WoWInternals.WoWCache.WoWCache.QuestFlags.Shareable); }
        }

        public bool IsDaily
        {
            get { return HasFlag(Styx.WoWInternals.WoWCache.WoWCache.QuestFlags.Daily); }
        }

        public bool FlagsPVP
        {
            get { return HasFlag(Styx.WoWInternals.WoWCache.WoWCache.QuestFlags.FlagsPVP); }
        }

        public bool IsWeekly
        {
            get { return HasFlag(Styx.WoWInternals.WoWCache.WoWCache.QuestFlags.Weekly); }
        }

        public bool IsAutoAccepted
        {
            get { return HasFlag(Styx.WoWInternals.WoWCache.WoWCache.QuestFlags.AutoAccept); }
        }

        public bool IsAutoComplete
        {
            get { return HasFlag(Styx.WoWInternals.WoWCache.WoWCache.QuestFlags.AutoComplete); }
        }

        private bool HasFlag(Styx.WoWInternals.WoWCache.WoWCache.QuestFlags flag)
        {
            return ((ulong)InternalInfo.Flags & (ulong)(long)flag) != 0UL;
        }

        private int CalculateRewardXp()
        {
            int level = StyxWoW.Me.Level;
            int num = Level;
            if (num == -1)
            {
                num = level;
            }

            WoWDb.Row row = StyxWoW.Db[Styx.Patchables.ClientDb.QuestXP].GetRow((uint)num);
            if (row == null)
            {
                return 0;
            }

            int num2 = Clamp(2 * (num - level) + 20, 1, 10);
            int num3 = num2 * row.GetField<int>(1U + InternalInfo.Level) / 10;
            if (num3 <= 100)
            {
                return (num3 + 2) / 5 * 5;
            }

            if (num3 <= 500)
            {
                return (num3 + 5) / 10 * 10;
            }

            if (num3 <= 1000)
            {
                return (num3 + 12) / 25 * 25;
            }

            return (num3 + 25) / 50 * 50;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                value = min;
            }
            else if (value > max)
            {
                value = max;
            }

            return value;
        }

        public List<QuestObjective> GetObjectives()
        {
            if (_questObjectives == null)
            {
                _questObjectives = new List<QuestObjective>();
                foreach (Styx.WoWInternals.WoWCache.WoWCache.QuestObjectiveInfo questObjectiveInfo in InternalInfo.GetObjectives())
                {
                    _questObjectives.Add(new QuestObjective((int)questObjectiveInfo.QuestLogObjectiveIndex, questObjectiveInfo.ObjectiveId, questObjectiveInfo.RequiredCount, questObjectiveInfo.Text, questObjectiveInfo.ObjectiveType));
                }
            }

            return _questObjectives;
        }

        public Styx.WoWInternals.WoWCache.WoWCache.QuestRewardChoice[] GetRewardChoices()
        {
            return _questRewardChoices;
        }

        public override string ToString()
        {
            return Name;
        }

        public static Quest FromId(uint id)
        {
            if (id == 0U)
            {
                return null;
            }

            Styx.WoWInternals.WoWCache.WoWCache.InfoBlock infoBlockById = StyxWoW.Cache[Styx.WoWInternals.WoWCache.CacheDb.Quest].GetInfoBlockById(id);
            if (infoBlockById != null)
            {
                return new Quest(infoBlockById.Quest);
            }

            return null;
        }

        private readonly string _completionText;

        private readonly string _description;

        private readonly string _name;

        private readonly string _objectiveText;

        private readonly string[] _objectives;

        private WoWSpell _rewardSpell;

        private readonly string _subDescription;

        private List<QuestObjective> _questObjectives;

        private Styx.WoWInternals.WoWCache.WoWCache.QuestRewardChoice[] _questRewardChoices;

        public enum QuestObjectiveType : byte
        {
            KillMob,
            CollectItem,
            UseGameObject,
            CollectIntermediateItem,
            AcquireCurrency,
            AcquireSpell,
            AcquireFactionValue,
            AcquireFactionValue2,
            Coinage,
            KillPlayers,
            SpecialA,
            SpecialB
        }

        public struct QuestObjective
        {
            public QuestObjective(int index, uint id, uint count, string objective, QuestObjectiveType type)
            {
                Index = index;
                ID = id;
                Count = count;
                Objective = objective;
                Type = type;
            }

            public bool IsEmpty
            {
                get { return ID == 0U; }
            }

            public uint Count;

            public uint ID;

            public int Index;

            public string Objective;

            public QuestObjectiveType Type;
        }
    }
}
