using System;
using System.Runtime.InteropServices;

namespace Styx.WoWInternals
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 16)]
    public struct NativeLuaTValue
    {
        public NativeLuaValue Value;

        public LuaType Type;

        private uint _unk;
    }
}
