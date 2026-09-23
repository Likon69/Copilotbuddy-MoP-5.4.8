using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Buddy.Overlay.Controls;
using Buddy.Overlay.Internal;
using Buddy.Overlay.Notifications;

namespace Buddy.Overlay
{
    public class OverlayManager
    {
        public OverlayManager(Process attachedProcess, ToastSettings toastSettings = null, Dispatcher customDispatcher = null)
        {
            this.ToastSettings = toastSettings ?? new ToastSettings(false);
            this.AttachedProcess = attachedProcess;
            this.customDispatcher = customDispatcher;
            this.addToastAction = new Action<ToastUIComponent>(this.OnAddToast);
            this.addWidgetAction = new Action<OverlayUIComponent>(this.OnAddWidget);
            this.removeWidgetAction = new Action<OverlayUIComponent>(this.OnRemoveWidget);
        }

        public ToastSettings ToastSettings { get; private set; }

        public Process AttachedProcess { get; private set; }

        public Dispatcher Dispatcher
        {
            get
            {
                Dispatcher dispatcher;
                if ((dispatcher = this.customDispatcher) == null)
                {
                    if (this.overlayThread == null)
                    {
                        return null;
                    }
                    dispatcher = Dispatcher.FromThread(this.overlayThread);
                }
                return dispatcher;
            }
        }

        public bool IsActive
        {
            get
            {
                return this.customDispatcher != null || this.overlayThread != null;
            }
        }

        public bool IsDesktopCompositionEnabled
        {
            get
            {
                bool flag;
                return NativeMethods.DwmIsCompositionEnabled(out flag) == 0 && flag;
            }
        }

        public double UnscaledOverlayWidth
        {
            get
            {
                return (double)Screen.PrimaryScreen.Bounds.Width;
            }
        }

        public double UnscaledOverlayHeight
        {
            get
            {
                return (double)Screen.PrimaryScreen.Bounds.Height;
            }
        }

        private void OnControlPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (NativeMethods.GetForegroundWindow() != this.AttachedProcess.MainWindowHandle)
            {
                NativeMethods.SetForegroundWindow(this.AttachedProcess.MainWindowHandle);
                this.UpdateOverlayPosition();
            }
            OverlayControl overlayControl = (OverlayControl)sender;
            if (object.Equals(this.lastClickedControl, overlayControl))
            {
                return;
            }
            this.lastClickedControl = overlayControl;
            int count = this.widgets.Count;
            System.Windows.Controls.Panel.SetZIndex(overlayControl, count);
            foreach (OverlayUIComponent overlayUIComponent in this.widgets)
            {
                OverlayControl control = overlayUIComponent.Control;
                int zindex = System.Windows.Controls.Panel.GetZIndex(control);
                if (zindex > 0 && !control.Equals(overlayControl))
                {
                    System.Windows.Controls.Panel.SetZIndex(control, zindex - 1);
                }
            }
        }

        private IntPtr LowLevelKeyboardCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            System.Windows.Controls.Primitives.TextBoxBase textBoxBase = Keyboard.FocusedElement as System.Windows.Controls.Primitives.TextBoxBase;
            if (nCode >= 0 && NativeMethods.GetForegroundWindow() == this.AttachedProcess.MainWindowHandle && this.lastClickedControl != null && textBoxBase != null)
            {
                uint num = (uint)Marshal.ReadInt32(lParam);
                uint num2 = NativeMethods.MapVirtualKey(num, 0U);
                WindowMessage windowMessage = (WindowMessage)(int)wParam;
                IntPtr intPtr = (IntPtr)(((windowMessage == WindowMessage.KEY_DOWN) ? 1L : -1073741823L) | (long)((ulong)((ulong)num2 << 16)));
                MSG msg = new MSG
                {
                    hwnd = this.widgetWindow.Handle,
                    lParam = intPtr,
                    message = (int)wParam,
                    wParam = (IntPtr)((long)((ulong)num))
                };
                bool flag = ComponentDispatcher.RaiseThreadMessage(ref msg);
                if (this.lastClickedControl != null && windowMessage == WindowMessage.KEY_DOWN && num == 27U)
                {
                    this.lastClickedControl = null;
                    Keyboard.ClearFocus();
                    flag = true;
                }
                if (!flag && windowMessage == WindowMessage.KEY_DOWN)
                {
                    NativeMethods.GetKeyboardState(this.keyboardState);
                    byte[] array = new byte[2];
                    if (NativeMethods.ToAscii(num, num2, this.keyboardState, array, 0U) == 1)
                    {
                        msg.message = 258;
                        msg.wParam = (IntPtr)((int)array[0]);
                        flag = ComponentDispatcher.RaiseThreadMessage(ref msg);
                    }
                }
                if (flag)
                {
                    return (IntPtr)1;
                }
            }
            return NativeMethods.CallNextHookEx(this.keyboardHookHandle, nCode, wParam, lParam);
        }

        private void OnWidgetWindowClosing(object sender, CancelEventArgs e)
        {
            this.UninstallKeyboardHook();
        }

        private void OnPulse(object sender, EventArgs e)
        {
            if (this.AttachedProcess.HasExited)
            {
                return;
            }
            for (int i = this.activeToasts.Count - 1; i >= 0; i--)
            {
                ToastUIComponent toastUIComponent = this.activeToasts[i];
                if (toastUIComponent.Stopwatch.Elapsed > toastUIComponent.DisplayDuration)
                {
                    this.activeToasts.RemoveAt(i);
                    this.toastWindow.RemoveToastElement(toastUIComponent.GuiElement);
                }
                else
                {
                    toastUIComponent.Update();
                }
            }
            foreach (OverlayUIComponent overlayUIComponent in this.widgets)
            {
                overlayUIComponent.Update();
            }
            this.UpdateOverlayPosition();
        }

        private void OnPreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (e.OldFocus is System.Windows.Controls.Primitives.TextBoxBase && !(e.NewFocus is System.Windows.Controls.Primitives.TextBoxBase))
            {
                this.UninstallKeyboardHook();
            }
        }

        private void OnPreviewGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (e.NewFocus is System.Windows.Controls.Primitives.TextBoxBase)
            {
                this.InstallKeyboardHook();
            }
        }

        private unsafe IntPtr ToastWindowMessageHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 71)
            {
                WINDOWPOS* ptr = (WINDOWPOS*)(void*)lParam;
                this.widgetWindow.SetPosition(ptr->x, ptr->y, ptr->cx, ptr->cy, ptr->hwnd);
            }
            return IntPtr.Zero;
        }

        private void InstallKeyboardHook()
        {
            if (this.keyboardHookHandle != IntPtr.Zero)
            {
                return;
            }
            this.keyboardHookHandle = NativeMethods.SetWindowsHookEx(WindowsHookType.WH_KEYBOARD_LL, this.keyboardHookProc, IntPtr.Zero, 0U);
            this.keyboardState = new byte[256];
        }

        private void UninstallKeyboardHook()
        {
            if (this.keyboardHookHandle == IntPtr.Zero)
            {
                return;
            }
            NativeMethods.UnhookWindowsHookEx(this.keyboardHookHandle);
            this.keyboardHookHandle = IntPtr.Zero;
            this.keyboardState = null;
        }

        public void AddUIComponent(OverlayUIComponent uiElement)
        {
            lock (this)
            {
                if (this.IsActive)
                {
                    if (!this.isActive)
                    {
                        this.pendingComponents.Add(uiElement);
                    }
                    else
                    {
                        this.Dispatcher.BeginInvoke(this.addWidgetAction, new object[] { uiElement });
                    }
                }
            }
        }

        public void RemoveUIComponent(OverlayUIComponent uiElement)
        {
            lock (this)
            {
                if (this.IsActive)
                {
                    if (!this.isActive)
                    {
                        this.pendingComponents.Remove(uiElement);
                    }
                    else
                    {
                        this.Dispatcher.BeginInvoke(this.removeWidgetAction, new object[] { uiElement });
                    }
                }
            }
        }

        public void AddToast(string text, int durationMs = 7000)
        {
            this.AddToast(new Func<string>(() => text), TimeSpan.FromMilliseconds((double)durationMs), Colors.Gold, Colors.Lime, new System.Windows.Media.FontFamily("Times New Roman"));
        }

        public void AddToast(Func<string> textProducer, TimeSpan duration, System.Windows.Media.Color color, System.Windows.Media.Color shadowColor, System.Windows.Media.FontFamily fontFamily)
        {
            this.AddToast(new TextToastComponent(textProducer, duration, color, shadowColor, fontFamily, FontWeights.UltraBold, 40.0));
        }

        public void AddToast(Func<string> textProducer, TimeSpan duration, System.Windows.Media.Color color, System.Windows.Media.Color shadowColor, System.Windows.Media.FontFamily fontFamily, FontWeight fontWeight, double fontSize)
        {
            this.AddToast(new TextToastComponent(textProducer, duration, color, shadowColor, fontFamily, fontWeight, fontSize));
        }

        public void AddToast(ToastUIComponent toast)
        {
            if (toast.DisplayDuration.TotalMilliseconds > 0.0 && toast.DisplayDuration.TotalMilliseconds <= 25000.0)
            {
                lock (this)
                {
                    if (!this.IsActive)
                    {
                        return;
                    }
                    if (!this.isActive)
                    {
                        this.pendingComponents.Add(toast);
                        return;
                    }
                    this.Dispatcher.BeginInvoke(this.addToastAction, new object[] { toast });
                    return;
                }
            }
            throw new ArgumentException(string.Format("duration needs to be bigger than 0 and smaller or equal to {0}", 25000));
        }

        public void Activate()
        {
            lock (this)
            {
                if (!this.isActive && !this.isActivating)
                {
                    this.isActivating = true;
                    if (this.customDispatcher != null)
                    {
                        this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(this.InitializeOverlay));
                    }
                    else
                    {
                        this.overlayThread = new Thread(new ThreadStart(this.RunOverlayThread))
                        {
                            IsBackground = true
                        };
                        this.overlayThread.SetApartmentState(ApartmentState.STA);
                        this.overlayThread.Start();
                    }
                }
            }
        }

        public void Deactivate()
        {
            lock (this)
            {
                if (this.IsActive)
                {
                    if (this.isActive && this.customDispatcher == null)
                    {
                        this.Dispatcher.BeginInvokeShutdown(DispatcherPriority.Render);
                    }
                    this.overlayThread = null;
                    this.isActive = false;
                }
            }
        }

        private void InitializeOverlay()
        {
            lock (this)
            {
                try
                {
                    this.overlayThreadId = NativeMethods.GetCurrentThreadId();
                    this.widgetWindow = new WidgetOverlayWindow(this.AttachedProcess);
                    this.toastWindow = new ToastOverlayWindow(this.AttachedProcess, this.ToastSettings);
                    this.widgetWindow.Closing += this.OnWidgetWindowClosing;
                    this.widgetWindow.PreviewGotKeyboardFocus += this.OnPreviewGotKeyboardFocus;
                    this.widgetWindow.PreviewLostKeyboardFocus += this.OnPreviewLostKeyboardFocus;
                    this.widgetWindow.Show();
                    this.toastWindow.Show();
                    (PresentationSource.FromVisual(this.toastWindow) as HwndSource).AddHook(new HwndSourceHook(this.ToastWindowMessageHook));
                    this.keyboardHookProc = new LowLevelKeyboardProc(this.LowLevelKeyboardCallback);
                    this.widgetWindow.Pulse += this.OnPulse;
                    foreach (OverlayUIComponentBase overlayUIComponentBase in this.pendingComponents)
                    {
                        ToastUIComponent toastUIComponent = overlayUIComponentBase as ToastUIComponent;
                        if (toastUIComponent != null)
                        {
                            this.addToastAction(toastUIComponent);
                        }
                        else
                        {
                            this.addWidgetAction((OverlayUIComponent)overlayUIComponentBase);
                        }
                    }
                    this.pendingComponents.Clear();
                    this.isActive = true;
                }
                finally
                {
                    this.isActivating = false;
                }
            }
        }

        private void RunOverlayThread()
        {
            this.InitializeOverlay();
            Dispatcher.Run();
        }

        private void UpdateOverlayPosition()
        {
            IntPtr window = NativeMethods.GetWindow(this.AttachedProcess.MainWindowHandle, GetWindowRelationship.GwHwndprev);
            RECT rect;
            NativeMethods.GetClientRect(this.AttachedProcess.MainWindowHandle, out rect);
            System.Drawing.Point point;
            NativeMethods.ClientToScreen(this.AttachedProcess.MainWindowHandle, out point);
            rect.Left += point.X;
            rect.Right += point.X;
            rect.Top += point.Y;
            rect.Bottom += point.Y;
            if (NativeMethods.GetWindowThreadId(window) == this.overlayThreadId && rect == this.lastClientRect)
            {
                return;
            }
            this.lastClientRect = rect;
            int x = rect.Left;
            int y = rect.Top;
            int cx = rect.Right - rect.Left;
            int cy = rect.Bottom - rect.Top;
            this.toastWindow.SetPosition(x, y, cx, cy, window);
        }

        private void OnAddToast(ToastUIComponent toast)
        {
            if (this.activeToasts.Count >= 12)
            {
                this.activeToasts.RemoveAt(0);
                this.toastWindow.RemoveToastElementAt(0);
            }
            if (!this.toastWindow.AddToastElement(toast.GuiElement))
            {
                return;
            }
            toast.Stopwatch.Start();
            this.activeToasts.Add(toast);
        }

        private void OnAddWidget(OverlayUIComponent component)
        {
            IWidgetHost widgetHost;
            if (!component.IsHitTestable)
            {
                widgetHost = this.toastWindow;
            }
            else
            {
                widgetHost = this.widgetWindow;
            }
            if (!widgetHost.AddWidget(component.Control))
            {
                return;
            }
            component.Control.PreviewMouseDown += this.OnControlPreviewMouseDown;
            this.widgets.Add(component);
        }

        private void OnRemoveWidget(OverlayUIComponent component)
        {
            IWidgetHost widgetHost;
            if (!component.IsHitTestable)
            {
                widgetHost = this.toastWindow;
            }
            else
            {
                widgetHost = this.widgetWindow;
            }
            widgetHost.RemoveWidget(component.Control);
            component.Control.PreviewMouseDown -= this.OnControlPreviewMouseDown;
            this.widgets.Remove(component);
            if (component.Control.Equals(this.lastClickedControl))
            {
                this.lastClickedControl = null;
            }
        }

        private WidgetOverlayWindow widgetWindow;

        private ToastOverlayWindow toastWindow;

        private RECT lastClientRect;

        private uint overlayThreadId;

        private Thread overlayThread;

        private const int MaxToastDurationMs = 25000;

        private const int MaxVisibleToasts = 12;

        private readonly List<OverlayUIComponent> widgets = new List<OverlayUIComponent>();

        private readonly List<ToastUIComponent> activeToasts = new List<ToastUIComponent>();

        private readonly Action<ToastUIComponent> addToastAction;

        private readonly Action<OverlayUIComponent> addWidgetAction;

        private readonly Action<OverlayUIComponent> removeWidgetAction;

        private LowLevelKeyboardProc keyboardHookProc;

        private IntPtr keyboardHookHandle;

        private byte[] keyboardState;

        private readonly List<OverlayUIComponentBase> pendingComponents = new List<OverlayUIComponentBase>();

        private OverlayControl lastClickedControl;

        private bool isActive;

        private bool isActivating;

        private readonly Dispatcher customDispatcher;
    }
}
