using System;

namespace Styx.Offsets
{
    public enum WoWItemFields
    {
        Owner = 32,
        ContainedIn = 40,
        Creator = 48,
        GiftCreator = 56,
        StackCount = 64,
        Expiration = 68,
        SpellCharges = 72,
        DynamicFlags = 92,
        Enchantment = 96,
        PropertySeed = 252,
        RandomPropertiesID = 256,
        Durability = 260,
        MaxDurability = 264,
        CreatePlayedTime = 268,
        ModifiersMask = 272
    }
}
