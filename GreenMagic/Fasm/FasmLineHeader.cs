using System;
using System.Runtime.InteropServices;

namespace GreenMagic.Fasm
{
    [StructLayout(LayoutKind.Explicit)]
    public struct FasmLineHeader
    {
        [FieldOffset(0)]
        public IntPtr file_path;

        [FieldOffset(4)]
        public uint line_number;

        [FieldOffset(8)]
        public uint file_offset;

        [FieldOffset(8)]
        public uint macro_calling_line;

        [FieldOffset(12)]
        public IntPtr macro_line;
    }
}
