using System;

namespace Styx
{
    [Flags]
    internal enum UnitDynamicFlags
    {
        None = 0,
        UnkFlag_1 = 1 << 0,
        Lootable = 1 << 1,
        TrackUnit = 1 << 2,
        TaggedByOther = 1 << 3,
        TaggedByMe = 1 << 4,
        SpecialInfo = 1 << 5,
        Dead = 1 << 6,
        ReferAFriendLinked = 1 << 7,
        IsTappedByAllThreatLists = 1 << 8
    }

    [Flags]
    internal enum UnitFlags : uint
    {
        None = 0,
        Sitting = 0x1,
        Influenced = 0x4,
        PlayerControlled = 0x8,
        EvadeRunningBack = 0x10,
        Preparation = 0x20,
        PlusMob = 0x40,
        NotAttackable = 0x100,
        Looting = 0x400,
        PetInCombat = 0x800,
        PvPFlagged = 0x1000,
        Silenced = 0x2000,
        Pacified = 0x20000,
        Stunned = 0x40000,
        CanPerformAction_Mask1 = 0x60000,
        Combat = 0x80000,
        TaxiFlight = 0x100000,
        Disarmed = 0x200000,
        Confused = 0x400000,
        Fleeing = 0x800000,
        Possessed = 0x1000000,
        NotSelectable = 0x2000000,
        Skinnable = 0x4000000,
        Mounted = 0x8000000,
        Dazed = 0x20000000,
        Sheathe = 0x40000000
    }

    [Flags]
    internal enum UnitFlags2
    {
        FeignDeath = 0x1,
        NoModel = 0x2,
        Flag_0x4 = 0x4,
        Flag_0x8 = 0x8,
        Flag_0x10 = 0x10,
        Flag_0x20 = 0x20,
        ForceAutoRunForward = 0x40,
        Flag_0x80 = 0x80,
        Flag_0x400 = 0x400,
        Flag_0x800 = 0x800,
        Flag_0x1000 = 0x1000
    }
}
