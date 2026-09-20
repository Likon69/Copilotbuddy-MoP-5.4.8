using System;
using System.Runtime.InteropServices;

namespace Styx.WoWInternals
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 10)]
    public unsafe struct NativeLuaCommonHeader
    {
        private fixed byte _data[10];
    }
}
