using System;
using System.Runtime.InteropServices;

namespace Styx.WoWInternals
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct WoWDescriptorQuest
    {
        public uint Id;

        [MarshalAs(UnmanagedType.U4)]
        public WoWDescriptorQuestFlags Flags;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] ObjectivesDone;

        public uint SecondsBeforeFailed;

        private int dword14;

        private int dword18;

        private int dword1C;

        private int dword20;

        private int dword24;

        private int dword28;

        private int dword2C;

        private int dword30;

        private int dword34;

        private int dword38;
    }
}
