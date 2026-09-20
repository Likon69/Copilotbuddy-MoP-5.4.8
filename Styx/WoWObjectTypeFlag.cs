using System;

namespace Styx
{
    [Flags]
    public enum WoWObjectTypeFlag
    {
        Object = 0x1,
        Item = 0x2,
        Container = 0x4,
        Unit = 0x8,
        Player = 0x10,
        GameObject = 0x20,
        DynamicObject = 0x40,
        Corpse = 0x80,
        AreaTrigger = 0x100,
        AiGroup = 0x200
    }
}
