using System;
using System.Runtime.InteropServices;

namespace GreenMagic.Fasm
{
    [StructLayout(LayoutKind.Explicit)]
    public struct FasmState
    {
        [FieldOffset(0)]
        public int condition;

        [FieldOffset(4)]
        public int error_code;

        [FieldOffset(4)]
        public uint output_length;

        [FieldOffset(8)]
        public IntPtr output_data;

        [FieldOffset(8)]
        public IntPtr error_line;
    }
}
