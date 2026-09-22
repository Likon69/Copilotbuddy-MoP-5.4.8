using System;

namespace Buddy.Overlay.Internal
{
    internal enum GetWindowRelationship : uint
    {
        GwHwndfirst,
        GwHwndlast,
        GwHwndnext,
        GwHwndprev,
        GwOwner,
        GwChild,
        GwEnabledpopup
    }
}
