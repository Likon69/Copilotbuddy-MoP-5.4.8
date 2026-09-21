using System;
using System.Collections.Generic;
using System.Linq;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Styx.CommonBot.Profiles.Quest.Order
{
    public class ProfileHelperFunctionsBase
    {
        public LocalPlayer Me
        {
            get { return StyxWoW.Me; }
        }

        public WoWSkill SkinningSkill
        {
            get { return Me.GetSkill(SkillLine.Skinning); }
        }

        public WoWSkill MiningSkill
        {
            get { return Me.GetSkill(SkillLine.Mining); }
        }

        public WoWSkill HerbalismSkill
        {
            get { return Me.GetSkill(SkillLine.Herbalism); }
        }

        public bool OnTransport
        {
            get { return Me.IsOnTransport; }
        }

        public double DistanceToUnit(uint entry)
        {
            WoWUnit unit = ObjectManager.GetObjectsOfType<WoWUnit>(true, true).FirstOrDefault(u => u.Entry == entry);
            if (unit != null)
                return unit.Distance;

            return -1.0;
        }

        public int GetItemCount(int itemId)
        {
            return Me.CarriedItems.Where(item => item.Entry == (ulong)itemId).Sum(item => (int)item.StackCount);
        }

        public bool IsObjectiveComplete(int objectiveId, uint questId)
        {
            if (Me.QuestLog.GetQuestById(questId) == null)
                return false;

            int questLogIndex = Lua.GetReturnVal<int>("return GetQuestLogIndexByID(" + questId + ")", 0);
            return Lua.GetReturnVal<bool>("return GetQuestLogLeaderBoard(" + objectiveId + "," + questLogIndex + ")", 2);
        }

        public WoWObject Object(uint entry, ObjectFilterDelegate filter = null)
        {
            return ObjectManager.GetObjectsOfType<WoWObject>(true, false)
                .Where(o => o.Entry == entry && (filter == null || filter(o)))
                .OrderBy(o => o.DistanceSqr)
                .FirstOrDefault();
        }

        public WoWUnit Unit(uint entry, UnitFilterDelegate filter = null)
        {
            return ObjectManager.GetObjectsOfType<WoWUnit>(true, false)
                .Where(u => u.Entry == entry && (filter == null || filter(u)))
                .OrderBy(u => u.DistanceSqr)
                .FirstOrDefault();
        }

        public bool HasMinion(uint entry)
        {
            List<WoWUnit> minions = Me.Minions;
            return Me != null && minions != null && minions.Any(minion => minion != null && minion.Entry == entry);
        }

        public bool HasQuestAvailable(int objectId)
        {
            return HasQuestAvailable(objectId, _defaultQuestGiverStatuses);
        }

        public bool HasQuestAvailable(int objectId, string type)
        {
            HashSet<QuestGiverStatus> statuses = new HashSet<QuestGiverStatus>();
            foreach (string part in type.Split(','))
            {
                string trimmed = part.Trim();
                try
                {
                    statuses.Add((QuestGiverStatus)Enum.Parse(typeof(QuestGiverStatus), trimmed, true));
                }
                catch (ArgumentException)
                {
                    Logging.WriteDiagnostic("HasQuestAvailable called with invalid type: {0}", trimmed);
                }
            }

            return HasQuestAvailable(objectId, statuses);
        }

        public bool HasQuestAvailable(int objectId, params QuestGiverStatus[] statuses)
        {
            return HasQuestAvailable(objectId, new HashSet<QuestGiverStatus>(statuses));
        }

        public bool HasQuestAvailable(int objectId, HashSet<QuestGiverStatus> statuses)
        {
            return statuses.Count > 0 && ObjectManager.GetObjectsOfType<WoWObject>(true, true)
                .Where(o => o.Entry == (ulong)objectId)
                .Any(o => statuses.Contains(o.QuestGiverStatus));
        }

        public bool IsAchievementCompleted(int id)
        {
            List<string> returnValues = Lua.GetReturnValues(string.Format("local _,_,_,completed = GetAchievementInfo({0}) if completed then return '1' else return '0' end", id));
            return returnValues != null && returnValues.Count > 0 && returnValues[0] == "1";
        }

        public bool IsAchievementCompleted(int achievementId, int index)
        {
            List<string> returnValues = Lua.GetReturnValues(string.Format("local _,_,completed = GetAchievementCriteriaInfo({0},{1}) if completed then return '1' else return '0' end", achievementId, index));
            return returnValues != null && returnValues.Count > 0 && returnValues[0] == "1";
        }

        public bool HasItem(int itemId)
        {
            return Me.CarriedItems.Any(item => item.Entry == (ulong)itemId);
        }

        public bool HasSpell(string spellName)
        {
            return SpellManager.HasSpell(spellName);
        }

        public bool HasSpell(int spellId)
        {
            return SpellManager.HasSpell(spellId);
        }

        public bool HasQuest(uint questId)
        {
            return Me.QuestLog.ContainsQuest(questId);
        }

        public bool IsQuestCompleted(uint id)
        {
            PlayerQuest quest = Me.QuestLog.GetQuestById(id);
            if (quest != null)
                return quest.IsCompleted;

            return StyxWoW.Me.QuestLog.GetCompletedQuests().Contains(id);
        }

        public bool HasFaction(int factionId)
        {
            FactionStanding standing;
            return Me.GetFactionStanding((uint)factionId, out standing);
        }

        public int GetFactionReputation(int factionId)
        {
            FactionStanding standing;
            if (!Me.GetFactionStanding((uint)factionId, out standing))
                return 0;

            return standing.TotalReputation;
        }

        public FactionStanding GetFactionStanding(int factionId)
        {
            FactionStanding standing;
            Me.GetFactionStanding((uint)factionId, out standing);
            return standing;
        }

        public uint GetCurrencyAmount(uint currencyId)
        {
            WoWCurrency currency = WoWCurrency.GetCurrencyById(currencyId);
            if (currency != null && currency.IsValid)
                return currency.Amount;

            return 0;
        }

        public bool CanFly()
        {
            return Flightor.CanFly;
        }

        public bool Chance(double val)
        {
            lock (_random)
                return _random.NextDouble() * 100.0 < val;
        }

        public int Random(int max)
        {
            return Random(0, max);
        }

        public int Random(int min, int max)
        {
            lock (_random)
                return _random.Next(min, max);
        }

        private static readonly HashSet<QuestGiverStatus> _defaultQuestGiverStatuses = new HashSet<QuestGiverStatus>
        {
            QuestGiverStatus.Available,
            QuestGiverStatus.AvailableRepeatable,
            QuestGiverStatus.LowLevelAvailable,
            QuestGiverStatus.LowLevelAvailableRepeatable
        };

        private static readonly Random _random = new Random(Environment.TickCount);

        public delegate bool ObjectFilterDelegate(WoWObject unit);

        public delegate bool UnitFilterDelegate(WoWUnit unit);
    }
}
