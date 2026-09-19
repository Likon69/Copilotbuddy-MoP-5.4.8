using System;

namespace Styx
{
    public enum WoWObjectType : uint
    {
        None,
        Item,
        Container,
        Unit,
        Player,
        GameObject,
        DynamicObject,
        Corpse,
        AreaTrigger,
        AiGroup
    }
}
