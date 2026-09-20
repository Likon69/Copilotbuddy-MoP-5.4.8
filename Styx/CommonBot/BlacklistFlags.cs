using System;

namespace Styx.CommonBot
{
    [Flags]
    public enum BlacklistFlags
    {
        Combat = 1,
        Loot = 2,
        Node = 4,
        Pull = 8,
        Interact = 16,
        [Obsolete("Use the ObjectDatabase flag instead")]
        NpcDatabase = 32,
        ObjectDatabase = 32,
        All = 63
    }
}
