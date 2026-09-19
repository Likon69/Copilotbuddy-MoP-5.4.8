using System;

namespace Styx.Offsets
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal struct VFTableIndex
    {
        public IntPtr GetPosition;

        public IntPtr GetName;
    }
}
