using System;
using System.Windows;

namespace Buddy.Overlay
{
    public abstract class OverlayUIComponentBase
    {
        protected OverlayUIComponentBase(bool isHitTestable)
        {
            this.IsHitTestable = isHitTestable;
        }

        public bool IsHitTestable { get; private set; }

        public abstract FrameworkElement GuiElement { get; }

        protected internal virtual void Update()
        {
        }
    }
}
