using System;
using System.Windows;

namespace Buddy.Overlay.Internal
{
    internal interface IToastHost
    {
        bool AddToastElement(FrameworkElement element);

        void RemoveToastElement(FrameworkElement element);

        void RemoveToastElementAt(int index);
    }
}
