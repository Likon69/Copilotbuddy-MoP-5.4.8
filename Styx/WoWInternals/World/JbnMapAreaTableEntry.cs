using System.Runtime.InteropServices;

namespace Styx.WoWInternals.World
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct JbnMapAreaTableEntry
    {
        public byte Flags;
        public fixed byte Padding[3];
        public uint Unknown4;
        public uint Unknown8;
        public uint UnknownC;
        public bool HasTileHere => (Flags & 1) != 0;
    }
}
