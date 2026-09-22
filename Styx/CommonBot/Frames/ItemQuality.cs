using System;

namespace Styx.CommonBot.Frames
{
    [Flags]
    public enum ItemQuality
    {
        None = 0,
        Poor = 1,
        Common = 2,
        Uncommon = 4,
        Rare = 8,
        Epic = 16,
        Legendary = 32,
        Artifact = 32,
        Heirloom = 64
    }
}
