using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;

namespace Buddy.Overlay.Internal
{
    internal abstract class OverlayWindowBase : TransparentWindow
    {
        protected OverlayWindowBase(Process attachedProcess, int windowStyle = 0)
        {
            this.Process = attachedProcess;
            this.extendedWindowStyle = windowStyle;
            this.ScaleTransform = new ScaleTransform(1.0, 1.0);
            this.ScreenBounds = Screen.PrimaryScreen.Bounds;
            this.Canvas = new Canvas
            {
                Background = System.Windows.Media.Brushes.Transparent,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch
            };
            this.Grid = new Grid
            {
                Background = System.Windows.Media.Brushes.Transparent,
                LayoutTransform = this.ScaleTransform,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch
            };
            this.Grid.Children.Add(this.Canvas);
            base.Content = this.Grid;
            CompositionTarget.Rendering += this.OnCompositionTargetRendering;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            this.Handle = new WindowInteropHelper(this).Handle;
            if (this.extendedWindowStyle != 0)
            {
                int num = NativeMethods.GetWindowLong(this.Handle, NativeMethods.GWL_EXSTYLE_OFFSET);
                num = num | this.extendedWindowStyle | NativeMethods.WS_EX_TOOLWINDOW;
                NativeMethods.SetWindowLong(this.Handle, NativeMethods.GWL_EXSTYLE_OFFSET, num);
            }
        }

        private void OnCompositionTargetRendering(object sender, EventArgs e)
        {
            if (this.isPulsing)
            {
                return;
            }
            try
            {
                this.isPulsing = true;
                this.OnPulse();
                EventHandler eventHandler = this.pulse;
                if (eventHandler != null)
                {
                    eventHandler(this, EventArgs.Empty);
                }
            }
            finally
            {
                this.isPulsing = false;
            }
        }

        protected Rectangle ScreenBounds { get; private set; }

        protected ScaleTransform ScaleTransform { get; private set; }

        protected Canvas Canvas { get; private set; }

        protected Grid Grid { get; private set; }

        protected internal void SetPosition(int x, int y, int cx, int cy, IntPtr insertAfter)
        {
            using (base.Dispatcher.DisableProcessing())
            {
                NativeMethods.SetWindowPos(this.Handle, insertAfter, x, y, cx, cy, SetWindowPosFlags.DoNotActivate);
                this.ScaleTransform.ScaleX = (double)((float)cx / (float)this.ScreenBounds.Width);
                this.ScaleTransform.ScaleY = (double)((float)cy / (float)this.ScreenBounds.Height);
            }
        }

        protected virtual void OnPulse()
        {
        }

        public event EventHandler Pulse
        {
            [CompilerGenerated]
            add
            {
                EventHandler eventHandler = this.pulse;
                EventHandler eventHandler2;
                do
                {
                    eventHandler2 = eventHandler;
                    EventHandler eventHandler3 = (EventHandler)Delegate.Combine(eventHandler2, value);
                    eventHandler = Interlocked.CompareExchange<EventHandler>(ref this.pulse, eventHandler3, eventHandler2);
                }
                while (eventHandler != eventHandler2);
            }
            [CompilerGenerated]
            remove
            {
                EventHandler eventHandler = this.pulse;
                EventHandler eventHandler2;
                do
                {
                    eventHandler2 = eventHandler;
                    EventHandler eventHandler3 = (EventHandler)Delegate.Remove(eventHandler2, value);
                    eventHandler = Interlocked.CompareExchange<EventHandler>(ref this.pulse, eventHandler3, eventHandler2);
                }
                while (eventHandler != eventHandler2);
            }
        }

        public IntPtr Handle { get; private set; }

        public Process Process { get; private set; }

        public double ScaleX
        {
            get
            {
                return this.ScaleTransform.ScaleX;
            }
        }

        public double ScaleY
        {
            get
            {
                return this.ScaleTransform.ScaleY;
            }
        }

        private readonly int extendedWindowStyle;

        private bool isPulsing;

        private EventHandler pulse;
    }
}
