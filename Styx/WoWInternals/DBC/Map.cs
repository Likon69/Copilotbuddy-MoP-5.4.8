using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Styx.Patchables;
using Tripper.Tools.Math;

namespace Styx.WoWInternals.DBC
{
    public class Map
    {
        private Map(uint id, WoWDb.Row row)
        {
            _entry = row.GetStruct<MapEntry>();
            InternalName = row.GetStringField<MapEntry>("InternalName");
            Name = row.GetStringField<MapEntry>("Name");
            MapDescription = row.GetStringField<MapEntry>("Description0");
            MapDescription2 = row.GetStringField<MapEntry>("Description1");
        }

        public uint MapId
        {
            get { return _entry.MapId; }
        }

        public string InternalName { get; private set; }

        public MapType MapType
        {
            get { return _entry.MapType; }
        }

        public uint MapFlags
        {
            get { return _entry.MapFlags; }
        }

        public bool IsBattleground
        {
            get { return MapType == MapType.Battleground; }
        }

        public string Name { get; private set; }

        public uint AreaTableId
        {
            get { return _entry.AreaTableId; }
        }

        public string MapDescription { get; private set; }

        public string MapDescription2 { get; private set; }

        public uint LoadingScreenId
        {
            get { return _entry.LoadingScreenId; }
        }

        public float BattlefieldMapIconScale
        {
            get { return _entry.BattlefieldMapIconScale; }
        }

        public Map GhostEntranceMap
        {
            get { return Map.FromId((uint)_entry.GhostEntranceMap); }
        }

        public Vector2 GhostEntranceLocation
        {
            get { return new Vector2(_entry.GhostEntranceX, _entry.GhostEntranceY); }
        }

        public uint TimeOfDayOverride
        {
            get { return _entry.TimeOfDayOverride; }
        }

        public uint ExpansionId
        {
            get { return _entry.ExpansionId; }
        }

        public uint RaidOffset
        {
            get { return _entry.RaidOffset; }
        }

        public uint MaxPlayers
        {
            get { return _entry.MaxPlayers; }
        }

        public bool IsArena
        {
            get { return MapType == MapType.Arena; }
        }

        public bool IsInstance
        {
            get { return MapType == MapType.Instance; }
        }

        public bool IsRaid
        {
            get { return MapType == MapType.Raid; }
        }

        public bool IsScenario
        {
            get { return MapType == MapType.Scenario; }
        }

        public bool IsDungeon
        {
            get { return IsInstance || IsRaid; }
        }

        public bool IsMountAllowed
        {
            get { return !IsDungeon || _mountableDungeons.Contains(MapId); }
        }

        public bool IsContinent
        {
            get { return !IsDungeon || _continents.Contains(MapId); }
        }

        public static Map FromId(uint id)
        {
            WoWDb.Row row = StyxWoW.Db[ClientDb.Map].GetRow(id);
            if (row == null)
            {
                return null;
            }
            return new Map(id, row);
        }

        public override string ToString()
        {
            return InternalName;
        }

        private readonly MapEntry _entry;

        private readonly List<uint> _mountableDungeons = new List<uint>
        {
            209, 269, 309, 509, 534, 560, 568, 580, 595, 603,
            615, 616, 1098, 1136
        };

        private readonly List<uint> _continents = new List<uint> { 0, 1, 530, 571 };

        #region Nested type: MapEntry

        [StructLayout(LayoutKind.Sequential)]
        private struct MapEntry
        {
            public override string ToString()
            {
                return string.Format("MapId: {0}, MapType: {1}, InternalName: {2}, Name: {3}", MapId, MapType, InternalName, Name);
            }

            public uint MapId;
            public IntPtr InternalName;
            public MapType MapType;
            public uint MapFlags;
            private readonly uint _unk4;
            public IntPtr Name;
            public uint AreaTableId;
            public IntPtr Description0;
            public IntPtr Description1;
            public uint LoadingScreenId;
            public float BattlefieldMapIconScale;
            public int GhostEntranceMap;
            public float GhostEntranceX;
            public float GhostEntranceY;
            public uint TimeOfDayOverride;
            public uint ExpansionId;
            public uint RaidOffset;
            public uint MaxPlayers;
            public uint ParentMapId;
        }

        #endregion
    }
}
