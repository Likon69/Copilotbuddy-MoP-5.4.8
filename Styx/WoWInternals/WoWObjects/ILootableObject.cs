using System;

namespace Styx.WoWInternals.WoWObjects
{
    public interface ILootableObject
    {
        bool CanLoot { get; }
    }
}
