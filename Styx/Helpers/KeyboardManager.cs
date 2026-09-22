using System;
using System.Runtime.InteropServices;
using Styx.Resources;

namespace Styx.Helpers
{
    public static class KeyboardManager
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, EntryPoint = "SendMessage")]
        private static extern IntPtr SendMessageUInt32(IntPtr windowHandle, uint message, uint wParam, uint lParam);

        [DllImport("user32.dll", EntryPoint = "PostMessage", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PostMessageNative(HandleRef windowHandle, uint message, uint wParam, uint lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, EntryPoint = "SendMessage")]
        private static extern IntPtr SendMessageIntPtr(IntPtr windowHandle, int message, IntPtr wParam, IntPtr lParam);

        public static void SendMessage(uint msg, uint wParam, uint lParam)
        {
            KeyboardManager.SendMessageUInt32(StyxWoW.Memory.Process.MainWindowHandle, msg, wParam, lParam);
        }

        public static void PostMessage(uint msg, uint wParam, uint lParam)
        {
            HandleRef handleRef = new HandleRef(null, StyxWoW.Memory.Process.MainWindowHandle);
            Console.WriteLine(Styx.Resources.StyxResources.PostMessage___0_, KeyboardManager.PostMessageNative(handleRef, msg, wParam, lParam));
        }

        public static void DownArrow()
        {
            KeyboardManager.SendMessage(256U, 40U, 22020097U);
            KeyboardManager.SendMessage(257U, 40U, 22020097U);
        }

        public static void PressKey(char key)
        {
            KeyboardManager.PostMessage(256U, (uint)key, 0U);
        }

        public static void ReleaseKey(char key)
        {
            KeyboardManager.PostMessage(257U, (uint)key, 0U);
        }

        public static void KeyUpDown(char key)
        {
            KeyboardManager.PressKey(key);
            StyxWoW.Sleep(10);
            KeyboardManager.ReleaseKey(key);
        }

        public static void AntiAfk()
        {
            StyxWoW.ResetAfk();
        }

        public enum eVirtualKeyMessages
        {
            VK_LBUTTON = 1,
            VK_RBUTTON,
            VK_CANCEL,
            VK_MBUTTON,
            VK_XBUTTON1,
            VK_XBUTTON2,
            VK_BACK = 8,
            VK_TAB,
            VK_CLEAR = 12,
            VK_RETURN,
            VK_SHIFT = 16,
            VK_CONTROL,
            VK_MENU,
            VK_PAUSE,
            VK_CAPITAL,
            VK_KANA,
            VK_HANGEUL = 21,
            VK_HANGUL = 21,
            VK_JUNJA = 23,
            VK_FINAL,
            VK_HANJA,
            VK_KANJI = 25,
            VK_ESCAPE = 27,
            VK_CONVERT,
            VK_NONCONVERT,
            VK_ACCEPT,
            VK_MODECHANGE,
            VK_SPACE,
            VK_PRIOR,
            VK_NEXT,
            VK_END,
            VK_HOME,
            VK_LEFT,
            VK_UP,
            VK_RIGHT,
            VK_DOWN,
            VK_SELECT,
            VK_PRINT,
            VK_EXECUTE,
            VK_SNAPSHOT,
            VK_INSERT,
            VK_DELETE,
            VK_HELP,
            VK_LWIN = 91,
            VK_RWIN,
            VK_APPS,
            VK_SLEEP = 95,
            VK_NUMPAD0,
            VK_NUMPAD1,
            VK_NUMPAD2,
            VK_NUMPAD3,
            VK_NUMPAD4,
            VK_NUMPAD5,
            VK_NUMPAD6,
            VK_NUMPAD7,
            VK_NUMPAD8,
            VK_NUMPAD9,
            VK_MULTIPLY,
            VK_ADD,
            VK_SEPARATOR,
            VK_SUBTRACT,
            VK_DECIMAL,
            VK_DIVIDE,
            VK_F1,
            VK_F2,
            VK_F3,
            VK_F4,
            VK_F5,
            VK_F6,
            VK_F7,
            VK_F8,
            VK_F9,
            VK_F10,
            VK_F11,
            VK_F12,
            VK_F13,
            VK_F14,
            VK_F15,
            VK_F16,
            VK_F17,
            VK_F18,
            VK_F19,
            VK_F20,
            VK_F21,
            VK_F22,
            VK_F23,
            VK_F24,
            VK_NUMLOCK = 144,
            VK_SCROLL,
            VK_OEM_NEC_EQUAL,
            VK_OEM_FJ_JISHO = 146,
            VK_OEM_FJ_MASSHOU,
            VK_OEM_FJ_TOUROKU,
            VK_OEM_FJ_LOYA,
            VK_OEM_FJ_ROYA,
            VK_LSHIFT = 160,
            VK_RSHIFT,
            VK_LCONTROL,
            VK_RCONTROL,
            VK_LMENU,
            VK_RMENU,
            VK_BROWSER_BACK,
            VK_BROWSER_FORWARD,
            VK_BROWSER_REFRESH,
            VK_BROWSER_STOP,
            VK_BROWSER_SEARCH,
            VK_BROWSER_FAVORITES,
            VK_BROWSER_HOME,
            VK_VOLUME_MUTE,
            VK_VOLUME_DOWN,
            VK_VOLUME_UP,
            VK_MEDIA_NEXT_TRACK,
            VK_MEDIA_PREV_TRACK,
            VK_MEDIA_STOP,
            VK_MEDIA_PLAY_PAUSE,
            VK_LAUNCH_MAIL,
            VK_LAUNCH_MEDIA_SELECT,
            VK_LAUNCH_APP1,
            VK_LAUNCH_APP2,
            VK_OEM_1 = 186,
            VK_OEM_PLUS,
            VK_OEM_COMMA,
            VK_OEM_MINUS,
            VK_OEM_PERIOD,
            VK_OEM_2,
            VK_OEM_3,
            VK_OEM_4 = 219,
            VK_OEM_5,
            VK_OEM_6,
            VK_OEM_7,
            VK_OEM_8,
            VK_OEM_AX = 225,
            VK_OEM_102,
            VK_ICO_HELP,
            VK_ICO_00,
            VK_PROCESSKEY,
            VK_ICO_CLEAR,
            VK_PACKET,
            VK_OEM_RESET = 233,
            VK_OEM_JUMP,
            VK_OEM_PA1,
            VK_OEM_PA2,
            VK_OEM_PA3,
            VK_OEM_WSCTRL,
            VK_OEM_CUSEL,
            VK_OEM_ATTN,
            VK_OEM_FINISH,
            VK_OEM_COPY,
            VK_OEM_AUTO,
            VK_OEM_ENLW,
            VK_OEM_BACKTAB,
            VK_ATTN,
            VK_CRSEL,
            VK_EXSEL,
            VK_EREOF,
            VK_PLAY,
            VK_ZOOM,
            VK_NONAME,
            VK_PA1,
            VK_OEM_CLEAR,
            VK_0 = 48,
            VK_1,
            VK_2,
            VK_3,
            VK_4,
            VK_5,
            VK_6,
            VK_7,
            VK_8,
            VK_9,
            VK_MINUS = 45,
            VK_EQUALS = 61
        }

        public enum eActionBar
        {
            Bar1 = 1,
            Bar2,
            Bar3,
            Bar4,
            Bar5,
            Bar6
        }

        public enum eActionButton
        {
            Button1 = 1,
            Button2,
            Button3,
            Button4,
            Button5,
            Button6,
            Button7,
            Button8,
            Button9,
            Button10,
            Button11,
            Button12
        }
    }
}
