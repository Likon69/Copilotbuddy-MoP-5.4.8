using System;

namespace GreenMagic
{
    internal delegate byte[] CopiedBytesTransformerDelegate(IntPtr addrOfFirstByte, byte[] bytes, IntPtr addOfFirstInjectedByte);
}
