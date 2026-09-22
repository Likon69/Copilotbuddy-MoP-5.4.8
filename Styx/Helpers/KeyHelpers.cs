using System;
using System.Runtime.InteropServices;

namespace Styx.Helpers
{
    public static class KeyHelpers
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, uint wParam, uint lParam);

        public static void ControlSend(string text, IntPtr hWnd)
        {
            foreach (char character in text)
            {
                SendMessage(hWnd, 0x100U, character, 0U);
                SendMessage(hWnd, 0x102U, character, 0U);
                SendMessage(hWnd, 0x101U, character, 0U);
            }
        }
    }
}
