using System;

namespace Buddy.Overlay.Internal
{
    [Flags]
    internal enum SetWindowPosFlags : uint
    {
        SynchronousWindowPosition = 16384U,
        DeferErase = 8192U,
        DrawFrame = 32U,
        FrameChanged = 32U,
        HideWindow = 128U,
        DoNotActivate = 16U,
        DoNotCopyBits = 256U,
        IgnoreMove = 2U,
        DoNotChangeOwnerZOrder = 512U,
        DoNotRedraw = 8U,
        DoNotReposition = 512U,
        DoNotSendChangingEvent = 1024U,
        IgnoreResize = 1U,
        IgnoreZOrder = 4U,
        ShowWindow = 64U
    }
}
