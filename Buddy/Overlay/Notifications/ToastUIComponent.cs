using System;
using System.Diagnostics;

namespace Buddy.Overlay.Notifications
{
    public abstract class ToastUIComponent : OverlayUIComponentBase
    {
        protected ToastUIComponent()
            : base(false)
        {
            this.Stopwatch = new Stopwatch();
        }

        internal Stopwatch Stopwatch { get; private set; }

        public TimeSpan DisplayDuration { get; protected set; }
    }
}
