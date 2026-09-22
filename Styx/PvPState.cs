using System;

namespace Styx
{
    [Flags]
    public enum PvPState
    {
        None = 0,
        PVP = 1,
        FFAPVP = 4,
        InPvPSanctuary = 8
    }
}
