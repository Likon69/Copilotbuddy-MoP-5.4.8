using System;
using Buddy.Overlay.Controls;

namespace Buddy.Overlay.Internal
{
    internal interface IWidgetHost
    {
        bool AddWidget(OverlayControl control);

        void RemoveWidget(OverlayControl control);
    }
}
