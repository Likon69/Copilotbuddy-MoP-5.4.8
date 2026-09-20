using System;

namespace Styx.WoWInternals
{
    [Flags]
    public enum WoWSpellSchool
    {
        None = 0,
        Physical = 1,
        Holy = 2,
        Fire = 4,
        Nature = 8,
        Frost = 16,
        Shadow = 32,
        Arcane = 64
    }
}
