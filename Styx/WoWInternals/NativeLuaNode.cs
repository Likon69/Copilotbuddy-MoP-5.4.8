using System;
using System.Runtime.InteropServices;

namespace Styx.WoWInternals
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct NativeLuaNode
    {
        public NativeLuaTValue Value;

        public NativeLuaTKey Key;
    }
}
