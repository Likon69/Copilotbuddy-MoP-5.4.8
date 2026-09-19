using System;

namespace Styx.Offsets
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal struct StaticOffsets
    {
        public IntPtr LuaState;
        public IntPtr IsInGame;
        public IntPtr ClientConnection;
    }
}
