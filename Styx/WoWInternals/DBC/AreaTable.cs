using System;
using System.Runtime.InteropServices;
using Styx.Patchables;

namespace Styx.WoWInternals.DBC
{
    public class AreaTable
    {
        private AreaTable(uint id, WoWDb.Row row)
        {
            _entry = row.GetStruct<AreaTableEntry>();
            InternalAreaName = row.GetStringField<AreaTableEntry>("InternalAreaName");
            AreaName = row.GetStringField<AreaTableEntry>("AreaName");
        }

        public uint AreaId
        {
            get { return _entry.Id; }
        }

        public uint MapId
        {
            get { return _entry.MapId; }
        }

        public Map ParentMap
        {
            get { return Map.FromId(_entry.MapId); }
        }

        public AreaTable ParentArea
        {
            get { return AreaTable.FromId(ParentAreaId); }
        }

        public uint ParentAreaId
        {
            get { return _entry.Zone; }
        }

        public string AreaName { get; private set; }

        public string InternalAreaName { get; private set; }

        public override string ToString()
        {
            return AreaName ?? base.ToString();
        }

        public static AreaTable FromId(uint id)
        {
            return DbcCache<AreaTable>.GetOrAdd(ClientDb.AreaTable, id, row => new AreaTable(id, row));
        }

        private readonly AreaTableEntry _entry;

        #region Nested type: AreaTableEntry

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private unsafe struct AreaTableEntry
        {
            public uint Id;
            public uint MapId;
            public uint Zone;
            public uint ExploreFlag;
            public uint Flags;
            public uint SoundProviderPref;
            public uint SoundProviderPrefUnderwater;
            public uint AmbienceId;
            public uint ZoneMusic;
            public uint IntroSound;
            public IntPtr InternalAreaName;
            private uint _unk11;
            private uint _unk12;
            public IntPtr AreaName;
            public fixed uint _unk14[11];
        }

        #endregion
    }
}
