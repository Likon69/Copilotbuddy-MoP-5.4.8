using System;

namespace Styx.CommonBot
{
    [Flags]
    public enum PulseFlags : uint
    {
        Objects = 0x1,
        Plugins = 0x2,
        BotEvents = 0x8,
        Targeting = 0x20,
        Looting = 0x40,
        InfoPanel = 0x80,
        Lua = 0x100,
        CharacterManager = 0x200,
        All = Objects | Plugins | BotEvents | Targeting | Looting | InfoPanel | Lua | CharacterManager
    }
}
