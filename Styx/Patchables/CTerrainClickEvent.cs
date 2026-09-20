using System;
using System.Runtime.InteropServices;

namespace Styx.Patchables
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct CTerrainClickEvent
    {
        public ulong GUID;

        public WoWPoint Position;

        [MarshalAs(UnmanagedType.U4)]
        public MouseButton Button;
    }
}
