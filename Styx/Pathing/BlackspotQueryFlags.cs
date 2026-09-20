using System;

namespace Styx.Pathing
{
    [Flags]
    public enum BlackspotQueryFlags : uint
    {
        Static = 1U,
        Dynamic = 2U,
        All = 4294967295U
    }
}
