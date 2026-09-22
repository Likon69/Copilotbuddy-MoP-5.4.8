using System;
using System.Windows;
using Buddy.Overlay.Controls;

namespace Buddy.Overlay
{
    public abstract class OverlayUIComponent : OverlayUIComponentBase
    {
        protected OverlayUIComponent(bool isHitTestable)
            : base(isHitTestable)
        {
        }

        public sealed override FrameworkElement GuiElement
        {
            get
            {
                return this.Control;
            }
        }

        public abstract OverlayControl Control { get; }
    }
}
