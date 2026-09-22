using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Styx.WoWInternals.DBC;
using Tripper.MeshMisc;

namespace Styx.WoWInternals.World
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct AreaTableBounds
    {
        public int MinY;
        public int MinX;
        public int MaxY;
        public int MaxX;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct NativeAreaTable
    {
        public AreaTableBounds Bounds;
        public int Width;
        public int Height;
        public uint Unknown18;
        public IntPtr Entries;
        public uint Unknown20;
        public uint Unknown24;
    }

    public class WorldMap
    {
        private const int MaxNumSubMaps = 12;

        [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 112)]
        private unsafe struct NativeWorldMap
        {
            [FieldOffset(12)] public int MainMapId;
            [FieldOffset(24)] public fixed uint MapPointers[MaxNumSubMaps];
            [FieldOffset(72)] public NativeAreaTable AreaTable;
        }

        public WorldMap(IntPtr baseAddress) { BaseAddress = baseAddress; }

        public IntPtr BaseAddress { get; private set; }
        public int MainMapID => ReadNative().MainMapId;
        public Map MainMap => Map.FromId((uint)MainMapID);

        public int GetMapIdAt(WoWPoint point)
        {
            var map = GetMapAt(point);
            return map?.MapID ?? -1;
        }

        public JbnMap GetMapAt(WoWPoint point)
        {
            var tile = TileIdentifier.GetByPosition(point.X, point.Y);
            WorldMapAreaTableEntry entry;
            if (!GetAreaTable().TryGetEntry(tile.X, tile.Y, out entry) || !entry.HasAreaHere) return null;
            return GetMap(entry.MapIndex);
        }

        public unsafe IEnumerable<JbnMap> GetMaps()
        {
            var maps = new List<JbnMap>(MaxNumSubMaps);
            var native = ReadNative();
            for (var i = 0; i < MaxNumSubMaps; i++)
            {
                var address = native.MapPointers[i];
                if (address != 0) maps.Add(new JbnMap((IntPtr)address));
            }
            return maps;
        }

        public unsafe JbnMap GetMap(int index)
        {
            if (index < 0 || index >= MaxNumSubMaps) return null;
            var native = ReadNative();
            var address = native.MapPointers[index];
            return address == 0 ? null : new JbnMap((IntPtr)address);
        }

        public AreaTable<WorldMapAreaTableEntry> GetAreaTable() => new AreaTable<WorldMapAreaTableEntry>(ReadNative().AreaTable);
        private NativeWorldMap ReadNative() => StyxWoW.Memory.Read<NativeWorldMap>(BaseAddress);
    }
}
