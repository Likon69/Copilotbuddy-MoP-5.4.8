using System;

namespace Styx
{
    [Flags]
    public enum WoWSocketColor : uint
    {
        None = 0U,
        Meta = 1U,
        Red = 2U,
        Yellow = 4U,
        Blue = 8U
    }
}
