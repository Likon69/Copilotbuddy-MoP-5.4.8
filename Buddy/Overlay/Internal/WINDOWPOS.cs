using System;

namespace Buddy.Overlay.Internal
{
    internal struct WINDOWPOS
    {
        public IntPtr hwnd;

        public IntPtr hwndInsertAfter;

        public int x;

        public int y;

        public int cx;

        public int cy;

        public uint flags;
    }
}
