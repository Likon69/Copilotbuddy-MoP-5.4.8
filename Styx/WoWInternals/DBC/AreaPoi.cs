using System;
using System.Runtime.InteropServices;
using Styx.Common;
using Styx.Patchables;

namespace Styx.WoWInternals.DBC
{
    public class AreaPoi
    {
        private AreaPoi(uint id, WoWDb.Row row)
        {
            ID = id;
            _entry = row.GetStruct<AreaPoiEntry>();
            Name = row.GetStringField<AreaPoiEntry>("Name");
            Description = row.GetStringField<AreaPoiEntry>("Description");
        }

        public uint ID { get; private set; }

        public int NormalIcon
        {
            get { return _entry.Icons[0]; }
        }

        internal int[] Icons
        {
            get { return _entry.Icons; }
        }

        public Vector2 Location
        {
            get { return _entry.Location; }
        }

        public int MapID
        {
            get { return _entry.MapID; }
        }

        public bool ShowInBattleMap
        {
            get { return (_entry.Flags & IconFlags.ShowInBattlemap) != 0; }
        }

        public bool ShowInBg
        {
            get { return (_entry.Flags & IconFlags.Bg) != 0; }
        }

        public bool ShowInZone
        {
            get { return (_entry.Flags & IconFlags.Zone) != 0; }
        }

        public int AreaID
        {
            get { return _entry.AreaID; }
        }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public uint WorldStateID
        {
            get { return _entry.WorldStateID; }
        }

        public static AreaPoi FromId(uint id)
        {
            WoWDb.Row row = StyxWoW.Db[ClientDb.AreaPOI].GetRow(id);
            if (row == null)
            {
                return null;
            }
            return new AreaPoi(id, row);
        }

        private readonly AreaPoiEntry _entry;

        #region Nested type: AreaPoiEntry

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct AreaPoiEntry
        {
            public uint ID;

            public uint dword4;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
            public int[] Icons;

            public uint dword2C;

            public Vector2 Location;

            public int MapID;

            public IconFlags Flags;

            public int AreaID;

            public IntPtr Name;

            public IntPtr Description;

            public uint WorldStateID;
        }

        #endregion

        #region Nested type: IconFlags

        [Flags]
        public enum IconFlags
        {
            Zone = 4,
            Bg = 128,
            ShowInBattlemap = 512
        }

        #endregion
    }
}
