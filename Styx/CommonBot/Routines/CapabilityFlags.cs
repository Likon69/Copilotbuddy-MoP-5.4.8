using System;

namespace Styx.CommonBot.Routines
{
    [Flags]
    public enum CapabilityFlags : uint
    {
        None = 0x0,
        Movement = 0x1,
        MoveBehind = 0x2,
        Facing = 0x4,
        GapCloser = 0x8,
        Aoe = 0x10,
        Targeting = 0x20,
        PetSummoning = 0x40,
        PetUse = 0x80,
        SpecialAttacks = 0x100,
        Kiting = 0x200,
        OffensiveDispel = 0x400,
        DefensiveDispel = 0x800,
        All = 0xFFF
    }
}
