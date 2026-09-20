using System;
using System.Runtime.InteropServices;

namespace Styx.WoWInternals
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct NativeLuaTKey
    {
        public NativeLuaValue Value;

        public LuaType Type;

        private uint _unk;

        public IntPtr NextNodePtr;

        private uint _pad;
    }
}
