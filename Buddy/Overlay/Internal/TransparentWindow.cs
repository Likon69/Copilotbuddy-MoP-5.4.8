using System;
using System.Windows;
using System.Windows.Media;

namespace Buddy.Overlay.Internal
{
    internal abstract class TransparentWindow : Window
    {
        protected TransparentWindow()
        {
            base.ShowInTaskbar = false;
            base.AllowsTransparency = true;
            base.WindowStyle = WindowStyle.None;
            base.Background = Brushes.Transparent;
            base.Style = null;
        }
    }
}
