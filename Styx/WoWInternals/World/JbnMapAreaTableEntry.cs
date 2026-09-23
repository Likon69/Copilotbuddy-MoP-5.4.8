using System.Runtime.InteropServices;

namespace Styx.WoWInternals.World
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct JbnMapAreaTableEntry
    {
        public byte Flags;
        public fixed byte f1[3];
        public uint dword4;
        public uint dword8;
        public uint dwordC;
        public bool HasTileHere => (Flags & 1) != 0;
    }
}
