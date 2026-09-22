using System;

namespace Buddy.Overlay.Internal
{
    internal enum WindowMessage : uint
    {
        WINDOWPOSCHANGING = 70U,
        WINDOWPOSCHANGED,
        NCHITTEST = 132U,
        KEY_DOWN = 256U,
        KEY_UP,
        WM_CHAR,
        SYSKEYDOWN = 260U,
        SYSKEYUP,
        SYSCHAR,
        LBUTTONDOWN = 513U,
        LBUTTONUP,
        LBUTTONDBLCLK,
        RBUTTONDOWN,
        RBUTTONUP,
        RBUTTONDBLCLK,
        MBUTTONDOWN,
        MBUTTONUP
    }
}
