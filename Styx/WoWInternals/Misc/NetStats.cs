using System.Runtime.InteropServices;

namespace Styx.WoWInternals.Misc
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct NetStats
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        internal uint[] PingPacketsLatency;

        internal uint Unk;

        internal uint Unk2;

        public uint BytesSent;

        public uint BytesReceived;

        public uint StartTime;
    }
}
