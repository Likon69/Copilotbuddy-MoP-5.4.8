using System;
using System.Runtime.InteropServices;
using Styx.Patchables;

namespace Styx.WoWInternals.DBC
{
    public class MapDifficulty
    {
        private MapDifficulty(uint id, WoWDb.Row row)
        {
            _entry = row.GetStruct<MapDifficultyEntry>();
            AreaTriggerText = row.GetStringField<MapDifficultyEntry>("AreaTriggerText");
            DifficultyString = row.GetStringField<MapDifficultyEntry>("DifficultyString");
        }

        public uint Id
        {
            get { return _entry.Id; }
        }

        public uint MapId
        {
            get { return _entry.MapId; }
        }

        public uint Difficulty
        {
            get { return _entry.Difficulty; }
        }

        public string AreaTriggerText { get; private set; }

        public uint RaidDuration
        {
            get { return _entry.RaidDuration; }
        }

        public uint MaxPlayers
        {
            get { return _entry.MaxPlayers; }
        }

        public string DifficultyString { get; private set; }

        public static MapDifficulty FromId(uint id)
        {
            return DbcCache<MapDifficulty>.GetOrAdd(ClientDb.MapDifficulty, id, row => new MapDifficulty(id, row));
        }

        private readonly MapDifficultyEntry _entry;

        #region Nested type: MapDifficultyEntry

        [StructLayout(LayoutKind.Sequential)]
        public struct MapDifficultyEntry
        {
            public uint Id;
            public uint MapId;
            public uint Difficulty;
            public IntPtr AreaTriggerText;
            public uint RaidDuration;
            public uint MaxPlayers;
            public IntPtr DifficultyString;
        }

        #endregion
    }
}
