using System;

namespace Styx
{
    public enum LfgState
    {
        None,
        NotInLfg,
        InQueue,
        AbandonedInDungeon,
        Proposal,
        InDungeon,
        Suspended,
        RoleCheck
    }
}
