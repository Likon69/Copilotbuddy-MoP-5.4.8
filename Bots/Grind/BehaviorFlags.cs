using System;

namespace Bots.Grind
{
    [Flags]
    public enum BehaviorFlags
    {
        Death = 1,
        Combat = 2,
        Loot = 4,
        Vendor = 8,
        Roam = 16,
        Pull = 32,
        Rest = 64,
        FlightPath = 128,
        All = 255
    }
}
