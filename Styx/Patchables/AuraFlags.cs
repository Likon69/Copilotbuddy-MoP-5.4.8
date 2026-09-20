using System;

namespace Styx.Patchables
{
    [Flags]
    public enum AuraFlags : byte
    {
        None = 0,
        Passive = 1,
        Cancelable = 2,
        Active = 4,
        PlayerCasted = 8,
        Harmful = 16
    }
}
