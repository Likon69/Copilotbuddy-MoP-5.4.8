using System;
using System.Diagnostics;
using Buddy.Overlay.Controls;

namespace Buddy.Overlay.Internal
{
    internal sealed class WidgetOverlayWindow : OverlayWindowBase, IWidgetHost
    {
        public WidgetOverlayWindow(Process attachedProcess)
            : base(attachedProcess, 134217728)
        {
        }

        public bool AddWidget(OverlayControl control)
        {
            if (base.Canvas.Children.Contains(control))
            {
                return false;
            }
            base.Canvas.Children.Add(control);
            return true;
        }

        public void RemoveWidget(OverlayControl control)
        {
            base.Canvas.Children.Remove(control);
        }
    }
}
