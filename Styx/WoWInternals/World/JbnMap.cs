using System;
using System.Runtime.InteropServices;
using System.Text;
using Styx.WoWInternals.DBC;

namespace Styx.WoWInternals.World
{
    public class JbnMap
    {
        [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 660)]
        private unsafe struct NativeJbnMap
        {
            [FieldOffset(80)] public int MapId;
            [FieldOffset(88)] public uint Flags;
            [FieldOffset(92)] public fixed byte MapPath[256];
            [FieldOffset(348)] public fixed byte MapName[256];
            [FieldOffset(620)] public NativeAreaTable AreaTable;
        }

        public JbnMap(IntPtr baseAddress) { BaseAddress = baseAddress; }

        public IntPtr BaseAddress { get; private set; }
        public int MapID => ReadNative().MapId;
        public Map Map => Map.FromId((uint)MapID);
        public bool IsActive => (ReadNative().Flags & 4U) != 0;

        public unsafe string MapPath
        {
            get
            {
                var native = ReadNative();
                return ReadString(native.MapPath, 256);
            }
        }

        public unsafe string MapName
        {
            get
            {
                var native = ReadNative();
                return ReadString(native.MapName, 256);
            }
        }

        public AreaTable<JbnMapAreaTableEntry> GetAreaTable() => new AreaTable<JbnMapAreaTableEntry>(ReadNative().AreaTable);

        public bool HasAreaAt(int x, int y)
        {
            JbnMapAreaTableEntry entry;
            return GetAreaTable().TryGetEntry(x, y, out entry) && entry.HasTileHere;
        }

        private NativeJbnMap ReadNative() => StyxWoW.Memory.Read<NativeJbnMap>(BaseAddress);

        private static unsafe string ReadString(byte* bytes, int capacity)
        {
            var length = 0;
            while (length < capacity && bytes[length] != 0) length++;
            var value = new byte[length];
            Marshal.Copy((IntPtr)bytes, value, 0, length);
            return Encoding.UTF8.GetString(value);
        }
    }
}
