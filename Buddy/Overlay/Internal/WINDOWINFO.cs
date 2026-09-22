using System;
using System.Runtime.InteropServices;

namespace Buddy.Overlay.Internal
{
    internal struct WINDOWINFO
    {
        public WINDOWINFO(bool? filler)
        {
            this = default(WINDOWINFO);
            this.cbSize = WINDOWINFO.cachedSize;
        }

        public uint cbSize;

        public RECT rcWindow;

        public RECT rcClient;

        public uint dwStyle;

        public uint dwExStyle;

        public uint dwWindowStatus;

        public uint cxWindowBorders;

        public uint cyWindowBorders;

        public ushort atomWindowType;

        public ushort wCreatorVersion;

        private static readonly uint cachedSize = (uint)Marshal.SizeOf(typeof(WINDOWINFO));
    }
}
