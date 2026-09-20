using System;
using System.Runtime.InteropServices;

namespace Styx.WoWInternals
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 20)]
    public struct NativeLuaTString
    {
        public NativeLuaCommonHeader Header;

        private byte _reserved;

        private byte _pad;

        public uint Hash;

        public IntPtr Length;
    }
}
