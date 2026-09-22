using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Interop;

namespace Styx.Common
{
    public static class HotkeysManager
    {
        private const int WM_HOTKEY = 786;

        private static readonly List<Hotkey> _hotkeys = new List<Hotkey>();
        private static readonly Queue<Hotkey> PendingRegistrations = new Queue<Hotkey>();
        private static readonly Queue<Hotkey> PendingUnregistrations = new Queue<Hotkey>();
        private static bool _initialized;
        private static IntPtr _watchWindow;
        private static int _hotkeyId;
        private static Thread _hotkeyThread;

        public static IEnumerable<Hotkey> Hotkeys { get { return _hotkeys.AsReadOnly(); } }

        public static Hotkey Register(string name, Keys key, ModifierKeys modifierKeys, Action<Hotkey> callback)
        {
            var hotkey = new Hotkey(name, key, modifierKeys, _hotkeyId++, callback);
            PendingRegistrations.Enqueue(hotkey);
            _hotkeys.Add(hotkey);
            EnsureThread();
            return hotkey;
        }

        public static void Unregister(string name)
        {
            Unregister(_hotkeys.FirstOrDefault(hotkey => hotkey.Name == name));
        }

        public static void Unregister(Hotkey hotkey)
        {
            if (hotkey != null)
            {
                PendingUnregistrations.Enqueue(hotkey);
                _hotkeys.Remove(hotkey);
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr window, int id);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PeekMessage(out MSG message, IntPtr window, uint filterMin,
            uint filterMax, uint removeMessage);

        public static void Initialize(IntPtr windowHandleToWatch)
        {
            if (_initialized)
                return;

            _watchWindow = windowHandleToWatch;
            _initialized = true;
        }

        private static void InternalRegisterHotkey(Hotkey hotkey)
        {
            if (hotkey.Registered)
                return;

            if (RegisterHotKey(IntPtr.Zero, hotkey.Id, (uint)hotkey.ModifierKeys, (uint)hotkey.Key))
                hotkey.Registered = true;
        }

        private static void InternalUnregisterHotkey(Hotkey hotkey)
        {
            if (!hotkey.Registered)
                return;

            if (UnregisterHotKey(IntPtr.Zero, hotkey.Id))
                hotkey.Registered = false;
        }

        private static void EnsureThread()
        {
            if (_hotkeyThread == null)
            {
                _hotkeyThread = new Thread(HotkeyThread)
                {
                    Name = "Hotkey Processing Loop",
                    IsBackground = true
                };
                _hotkeyThread.Start();
            }
        }

        private static void HotkeyThread()
        {
            while (true)
            {
                if (PendingRegistrations.Count != 0)
                {
                    InternalRegisterHotkey(PendingRegistrations.Dequeue());
                    continue;
                }

                while (PendingUnregistrations.Count != 0)
                    InternalUnregisterHotkey(PendingUnregistrations.Dequeue());

                if (GetForegroundWindow() != _watchWindow)
                {
                    foreach (Hotkey hotkey in _hotkeys)
                        InternalUnregisterHotkey(hotkey);
                }
                else
                {
                    foreach (Hotkey hotkey in _hotkeys)
                        InternalRegisterHotkey(hotkey);

                    MessagePump();
                }

                Thread.Sleep(100);
            }
        }

        private static void MessagePump()
        {
            MSG message;
            while (PeekMessage(out message, IntPtr.Zero, WM_HOTKEY, WM_HOTKEY, 1U))
            {
                Hotkey hotkey = Hotkeys.FirstOrDefault(item => item.Id == (int)message.wParam);
                if (hotkey != null)
                {
                    Logging.WriteDiagnostic(hotkey.Name + " pressed.");
                    hotkey.Callback(hotkey);
                }
            }
        }
    }
}
