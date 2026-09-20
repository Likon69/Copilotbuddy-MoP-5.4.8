using System;

namespace Styx.WoWInternals
{
    public enum BattlegroundStatus : uint
    {
        None,
        Queued,
        Confirm,
        Active = 4U
    }
}
