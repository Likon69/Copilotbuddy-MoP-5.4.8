using System;

namespace Styx.WoWInternals.WoWObjects
{
    [Flags]
    public enum ReputationFlags : byte
    {
        None = 0, Visible = 1, AtWar = 2, Hidden = 4, ForcedInvisible = 8,
        ForcedPeace = 16, Inactive = 32, Flag_0x40 = 64, Expansion_2 = 128
    }
}
