using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Styx.Common
{
    [Obsolete("Use HotkeysManager instead. It provides much better handling for hotkeys.")]
    public static class Hotkeys
    {
        private static readonly Thread _worker;
        private static readonly Dictionary<string, RegisteredHotkey> Registry;
        private static IntPtr _currentTrackedWindow;

        static Hotkeys()
        {
            _worker = new Thread(() =>
            {
                while (true)
                {
                    Pulse();
                    Thread.Sleep(10);
                }
            })
            {
                IsBackground = true,
                Name = "Hotkey Detection Thread"
            };
            Registry = new Dictionary<string, RegisteredHotkey>();
            _worker.Start();
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(Keys key);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        public static void SetWindowHandle(IntPtr windowHandle)
        {
            _currentTrackedWindow = windowHandle;
        }

        public static bool RegisterHotkey(string hotkeyName, Action callback, params Keys[] keyCombo)
        {
            if (Registry.ContainsKey(hotkeyName))
                return false;

            var hotkey = new RegisteredHotkey
            {
                HotkeyName = hotkeyName,
                Callback = callback,
                Keys = keyCombo
            };
            Registry.Add(hotkeyName, hotkey);
            Logging.WriteDiagnostic("Registered Hotkey '" + hotkeyName + "'");
            return true;
        }

        public static bool RemoveHotkey(string hotkeyName)
        {
            if (Registry.ContainsKey(hotkeyName))
            {
                Registry.Remove(hotkeyName);
                return true;
            }

            return false;
        }

        internal static void Pulse()
        {
            if (_currentTrackedWindow == IntPtr.Zero || GetForegroundWindow() != _currentTrackedWindow)
                return;

            IOrderedEnumerable<RegisteredHotkey> pressed =
                GetPressedKeys().OrderByDescending(hotkey => hotkey.Keys.Length);
            RegisteredHotkey first = pressed.FirstOrDefault();
            if (first == null)
                return;

            foreach (RegisteredHotkey hotkey in pressed.Where(item => item.Keys.Length == first.Keys.Length))
            {
                try
                {
                    Logging.WriteDiagnostic("Hotkey pressed: " + hotkey.HotkeyName);
                    hotkey.Callback();
                    break;
                }
                catch (Exception ex)
                {
                    Logging.WriteDiagnostic("Exception calling callback for hotkey: " + hotkey.HotkeyName);
                    Logging.WriteException(ex);
                }
            }
        }

        private static List<RegisteredHotkey> GetPressedKeys()
        {
            var pressed = new List<RegisteredHotkey>();
            foreach (RegisteredHotkey hotkey in Registry.Values)
            {
                if (hotkey.Keys.All(key => (GetAsyncKeyState(key) & 32768) != 0))
                    pressed.Add(hotkey);
            }

            return pressed;
        }

        private class RegisteredHotkey
        {
            public Action Callback;
            public string HotkeyName;
            public Keys[] Keys;
        }
    }
}
