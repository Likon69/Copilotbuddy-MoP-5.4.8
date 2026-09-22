using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Styx.CommonBot.POI;
using Styx.Helpers;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Styx.CommonBot
{
    public static class LootPredictor
    {
        private const int DecisionTimeoutMilliseconds = 1000;
        private const int HistoryExpiryMilliseconds = 600000;
        private const int RecordReuseMilliseconds = 300000;
        private const double LootRateThreshold = 0.25;

        private static ulong _lastKillTargetGuid;
        private static readonly Dictionary<uint, EntryLootData> _entryData = new Dictionary<uint, EntryLootData>();
        private static readonly HashSet<ulong> _ignoredGuids = new HashSet<ulong>();

        public static bool UseLootPredictor { get; set; } = true;

        internal static bool WasKilledByMe(WoWUnit unit)
        {
            if (!UseLootPredictor)
                return unit.KilledByMe;

            return unit.IsDead && (unit.TaggedByMe || (!unit.IsTagged && unit.Guid == _lastKillTargetGuid));
        }

        private static LootRecord GetRecycledOrNewRecord(EntryLootData history)
        {
            foreach (LootRecord record in history.List)
            {
                if (ObjectManager.GetObjectByGuid(record.Guid) == null && record.FirstSeenTimer.ElapsedMilliseconds > RecordReuseMilliseconds)
                    return record;
            }

            var newRecord = new LootRecord();
            history.List.Add(newRecord);
            return newRecord;
        }

        private static EntryLootData GetEntryData(uint entry)
        {
            EntryLootData history;
            if (!_entryData.TryGetValue(entry, out history))
            {
                history = new EntryLootData { List = new List<LootRecord>() };
                _entryData[entry] = history;
            }

            history.LastSeen.Restart();
            return history;
        }

        [Conditional("DEBUG")]
        private static void DebugLog(string format, params object[] args)
        {
        }

        private static void PurgeExpiredEntries()
        {
            uint[] expired = _entryData.Where(pair => pair.Value.LastSeen.ElapsedMilliseconds > HistoryExpiryMilliseconds)
                .Select(pair => pair.Key).ToArray();
            foreach (uint entry in expired)
                _entryData.Remove(entry);
        }

        internal static bool CanLoot(WoWUnit unit)
        {
            if (!UseLootPredictor || StyxWoW.Me.GroupInfo.IsInParty || _ignoredGuids.Contains(unit.Guid))
                return unit.CanLoot;

            EntryLootData history = GetEntryData(unit.Entry);
            LootRecord record = history.List.FirstOrDefault(r => r.Guid == unit.Guid);
            if (record != null)
            {
                if (unit.CanLoot)
                {
                    if (!record.HasDecided)
                        record.HasDecided = true;

                    record.FinalResult = true;
                    return true;
                }

                if (record.HasDecided && record.FinalResult)
                    return false;

                if (record.FirstSeenTimer.ElapsedMilliseconds > DecisionTimeoutMilliseconds)
                {
                    if (!record.HasDecided)
                    {
                        record.FinalResult = false;
                        record.HasDecided = true;
                    }

                    return false;
                }

                return record.InitialResult;
            }
            else
            {
                LootRecord newRecord = GetRecycledOrNewRecord(history);
                newRecord.Guid = unit.Guid;
                newRecord.FirstSeenTimer.Restart();

                if (unit.CanLoot)
                {
                    newRecord.FinalResult = true;
                    newRecord.InitialResult = true;
                    newRecord.HasDecided = true;
                    return true;
                }

                int total = 0;
                int lootable = 0;
                foreach (LootRecord existing in history.List)
                {
                    bool result = existing.FinalResult;
                    if (!existing.HasDecided)
                        result = existing.InitialResult;

                    total++;
                    if (result)
                        lootable++;
                }

                bool prediction = total == 1 || (double)lootable / total >= LootRateThreshold;
                newRecord.HasDecided = false;
                newRecord.InitialResult = prediction;
                return prediction;
            }
        }

        internal static bool Initialize()
        {
            BotEvents.OnBotStarted += OnBotStarted;
            BotEvents.OnPulse += OnPulse;
            return true;
        }

        private static void OnPulse(BotEvents.PulseEventArgs args)
        {
            if (!UseLootPredictor)
                return;

            PurgeExpiredEntries();

            if (BotPoi.Current.Type == PoiType.Kill)
            {
                WoWUnit unit = BotPoi.Current.AsObject as WoWUnit;
                if (unit != null)
                    _lastKillTargetGuid = unit.Guid;
            }
        }

        private static void OnBotStarted(EventArgs args)
        {
            _ignoredGuids.Clear();
            if (UseLootPredictor)
            {
                _ignoredGuids.UnionWith(ObjectManager.GetObjectsOfType<WoWUnit>(false, false).Where(unit => unit.IsDead).Select(unit => unit.Guid));
            }
        }

        private class LootRecord
        {
            public LootRecord()
            {
                FirstSeenTimer = new Stopwatch();
            }

            public ulong Guid { get; set; }
            public Stopwatch FirstSeenTimer { get; private set; }
            public bool InitialResult { get; set; }
            public bool FinalResult { get; set; }
            public bool HasDecided { get; set; }
        }

        private class EntryLootData
        {
            public EntryLootData()
            {
                LastSeen = new Stopwatch();
            }

            public List<LootRecord> List { get; set; }
            public Stopwatch LastSeen { get; private set; }
        }
    }
}
