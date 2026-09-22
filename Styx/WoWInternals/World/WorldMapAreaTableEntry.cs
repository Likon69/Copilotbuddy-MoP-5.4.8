using System.Runtime.InteropServices;

namespace Styx.WoWInternals.World
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct WorldMapAreaTableEntry
    {
        public byte Flags;
        public sbyte MapIndex;
        public bool HasAreaHere => (Flags & 1) != 0;
    }
}
