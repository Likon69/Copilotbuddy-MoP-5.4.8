using System;

namespace Styx.WoWInternals.World
{
    [Flags]
    public enum TraceLineHitFlags
    {
        None = 0x0,
        DoodadCollision = 0x1,
        DoodadRender = 0x2,
        WmoCollision = 0x10,
        WmoRender = 0x20,
        WmoNoCamCollision = 0x40,
        Terrain = 0x100,
        NoWmoDoodad = 0x2000,
        LiquidWaterWalkable = 0x10000,
        LiquidAll = 0x20000,
        Cull = 0x80000,
        EntityCollision = 0x100000,
        Collision = EntityCollision | Terrain | WmoCollision | DoodadCollision,
        LineOfSight = Collision,
        SpellLineOfSight = EntityCollision | WmoCollision,
    }
}
