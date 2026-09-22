using System;

namespace Styx
{
    [Flags]
    public enum WoWStateFlag
    {
        None = 0,
        AlwaysStand = 1,
        Sneaking = 2,
        UnTrackable = 4
    }
}
