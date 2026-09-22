using System;
using System.Runtime.InteropServices;

namespace Styx.Common
{
    public static class Flash
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FlashWindowEx(ref FlashInfo flashInfo);

        public static bool FlashWindow(IntPtr hwnd, FlashFlags flags, int timeout = 0, int count = int.MaxValue)
        {
            var flashInfo = new FlashInfo
            {
                Size = Marshal.SizeOf(typeof(FlashInfo)),
                Hwnd = hwnd,
                Flags = (int)flags,
                Count = count,
                Timeout = timeout
            };

            return FlashWindowEx(ref flashInfo);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FlashInfo
        {
            public int Size;
            public IntPtr Hwnd;
            public int Flags;
            public int Count;
            public int Timeout;
        }
    }
}
