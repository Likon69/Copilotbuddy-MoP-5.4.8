using System;
using System.Runtime.InteropServices;

namespace Styx.WoWInternals
{
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 8)]
    public struct NativeLuaValue
    {
        [FieldOffset(0)]
        public IntPtr Pointer;

        [FieldOffset(0)]
        public double Number;

        [FieldOffset(0)]
        private int _b;

        public bool Bool
        {
            get { return _b != 0; }
        }
    }
}
