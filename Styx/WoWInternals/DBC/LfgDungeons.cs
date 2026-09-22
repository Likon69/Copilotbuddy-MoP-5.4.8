using System;
using Styx.Patchables;

namespace Styx.WoWInternals.DBC
{
    /// <summary>Respresent a dungeon.</summary>
    public class LfgDungeons
    {
        public struct LfgDungeonsEntry
        {
            public uint Id;
            public IntPtr DungeonName;
            public uint MinLevel;
            public uint MaxLevel;
            public uint RecommendedLevel;
            public uint QueueMinLevel;
            public uint QueueMaxLevel;
            public int MapId;
            public uint Difficulty;
            public uint Flags;
            public uint TypeId;
            public uint Faction;
            public IntPtr TextureFilename;
            public uint ExpansionId;
            public uint OrderIndex;
            public uint GroupId;
            public IntPtr Description;
            public uint ParentId;
            public int MaxTankCount;
            public int MaxHealerCount;
            public int MaxDpsCount;
            public int MinTankCount;
            public int MinHealerCount;
            public int MinDpsCount;
            public int ScenarioId;
            public int SubType;
            public int BonusRepAmount;
            public int Unk6C;
            public int Unk70;
        }

        private readonly LfgDungeonsEntry _entry;

        private LfgDungeons(uint id, WoWDb.Row row)
        {
            _entry = row.GetStruct<LfgDungeonsEntry>();
            Name = row.GetStringField<LfgDungeonsEntry>("DungeonName");
            TextureFilename = row.GetStringField<LfgDungeonsEntry>("TextureFilename");
            Description = row.GetStringField<LfgDungeonsEntry>("Description");
        }

        public uint Id { get { return _entry.Id; } }

        public string Name { get; private set; }

        public uint MinLevel { get { return _entry.MinLevel; } }

        public uint MaxLevel { get { return _entry.MaxLevel; } }

        public uint RecommendedLevel { get { return _entry.RecommendedLevel; } }

        public uint QueueMinLevel { get { return _entry.QueueMinLevel; } }

        public uint QueueMaxLevel { get { return _entry.QueueMaxLevel; } }

        public int MapId { get { return _entry.MapId; } }

        public Map Map { get { return Map.FromId((uint)MapId); } }

        public uint Difficulty { get { return _entry.Difficulty; } }

        private uint Flags { get { return _entry.Flags; } }

        public bool IsHolidayEvent { get { return (Flags & 4) != 0; } }

        public uint TypeId { get { return _entry.TypeId; } }

        public uint FactionId { get { return _entry.Faction; } }

        public Faction Faction { get { return Faction.FromId(FactionId); } }

        public string TextureFilename { get; private set; }

        public uint ExpansionId { get { return _entry.ExpansionId; } }

        public uint OrderIndex { get { return _entry.OrderIndex; } }

        public uint GroupId { get { return _entry.GroupId; } }

        public string Description { get; private set; }

        public static LfgDungeons FromId(uint id)
        {
            return DbcCache<LfgDungeons>.GetOrAdd(ClientDb.LfgDungeons, id, row => new LfgDungeons(id, row));
        }
    }
}
