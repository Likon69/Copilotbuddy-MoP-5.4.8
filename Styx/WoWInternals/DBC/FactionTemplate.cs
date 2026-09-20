using System;
using System.Runtime.InteropServices;
using Styx.Patchables;
using Styx.WoWInternals.WoWObjects;

namespace Styx.WoWInternals.DBC
{
    public class FactionTemplate
    {
        private FactionTemplate(uint id, WoWDb.Row row)
        {
            Id = id;
            Record = row.GetStruct<FactionTemplateDbcRecord>();
        }

        public uint Id { get; private set; }

        public Faction Faction
        {
            get { return Faction.FromId(Record.FactionId); }
        }

        public FactionTemplateDbcRecord Record { get; private set; }

        private WoWUnitReaction FactionReputationCompare(FactionStanding standing)
        {
            int totalReputation = standing.TotalReputation;
            if (totalReputation >= 42000)
            {
                return WoWUnitReaction.Exalted;
            }

            if (totalReputation >= 21000)
            {
                return WoWUnitReaction.Revered;
            }

            if (totalReputation >= 9000)
            {
                return WoWUnitReaction.Honored;
            }

            if (totalReputation >= 3000)
            {
                return WoWUnitReaction.Friendly;
            }

            if (totalReputation >= 0)
            {
                return WoWUnitReaction.Neutral;
            }

            if (totalReputation >= -3000)
            {
                return WoWUnitReaction.Unfriendly;
            }

            if (totalReputation >= -6000)
            {
                return WoWUnitReaction.Hostile;
            }

            return WoWUnitReaction.Hated;
        }

        public WoWUnitReaction GetReactionTowards(WoWUnit unit)
        {
            FactionTemplate factionTemplate = unit.FactionTemplate;
            if (factionTemplate != null)
            {
                WoWPlayer controllingPlayer;
                if (unit.PlayerControlled && (controllingPlayer = unit.ControllingPlayer) != null && controllingPlayer.IsMe)
                {
                    if ((Record.FactionFlags & 4096U) != 0U && controllingPlayer.ContestedPvPFlagged)
                    {
                        return WoWUnitReaction.Hostile;
                    }

                    WoWUnitReaction woWUnitReaction;
                    if (StyxWoW.Me.GetForcedReaction(Record.FactionId, out woWUnitReaction))
                    {
                        return woWUnitReaction;
                    }

                    FactionStanding factionStanding;
                    if (!unit.HasFlag(UnitFlags2.Flag_0x4) && StyxWoW.Me.GetFactionStanding(Faction, out factionStanding))
                    {
                        return FactionReputationCompare(factionStanding);
                    }
                }

                return GetReactionTowards(factionTemplate);
            }

            return WoWUnitReaction.Neutral;
        }

        public WoWUnitReaction GetReactionTowards(FactionTemplate otherFaction)
        {
            FactionTemplateDbcRecord record = Record;
            FactionTemplateDbcRecord record2 = otherFaction.Record;
            if ((record2.FactionGroupId & record.HostileMask) != 0U)
            {
                return WoWUnitReaction.Hostile;
            }

            int num = 0;
            while (num < 4 && record.EnemyFactions[num] != 0U)
            {
                if (record.EnemyFactions[num] == record2.FactionId)
                {
                    return WoWUnitReaction.Hostile;
                }

                num++;
            }

            if ((record2.FactionGroupId & record.FriendlyMask) != 0U)
            {
                return WoWUnitReaction.Friendly;
            }

            int num2 = 0;
            while (num2 < 4 && record.FriendlyFactions[num2] != 0U)
            {
                if (record.FriendlyFactions[num2] == record2.FactionId)
                {
                    return WoWUnitReaction.Friendly;
                }

                num2++;
            }

            if ((record.FactionGroupId & record2.FriendlyMask) != 0U)
            {
                return WoWUnitReaction.Friendly;
            }

            int num3 = 0;
            while (num3 < 4 && record2.FriendlyFactions[num3] != 0U)
            {
                if (record2.FriendlyFactions[num3] == record.FactionId)
                {
                    return WoWUnitReaction.Friendly;
                }

                num3++;
            }

            return (WoWUnitReaction)((~(record.FactionFlags >> 12) & 2U) | 1U);
        }

        public static FactionTemplate FromId(uint id)
        {
            return DbcCache<FactionTemplate>.GetOrAdd(ClientDb.FactionTemplate, id, row => new FactionTemplate(id, row));
        }

        #region Nested type: FactionTemplateDbcRecord

        public struct FactionTemplateDbcRecord
        {
            public uint Id;

            public uint FactionId;

            public uint FactionFlags;

            public uint FactionGroupId;

            public uint FriendlyMask;

            public uint HostileMask;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public uint[] EnemyFactions;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public uint[] FriendlyFactions;
        }

        #endregion
    }
}
