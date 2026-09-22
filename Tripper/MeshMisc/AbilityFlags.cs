using System;

namespace Tripper.MeshMisc
{
    [Flags]
    public enum AbilityFlags : ushort
    {
        None = 0,
        Run = 1,
        Swim = 4,
        Jump = 8,
        Unwalkable = 16,
        Teleport = 32,
        Transport = 64,
        Horde = 4096,
        Alliance = 8192,
        All = 65535
    }
}
