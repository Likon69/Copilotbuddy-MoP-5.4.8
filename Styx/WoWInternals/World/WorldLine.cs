using System;

namespace Styx.WoWInternals.World
{
    public struct WorldLine
    {
        public WoWPoint Start;

        public WoWPoint End;

        public WorldLine(WoWPoint start, WoWPoint end)
        {
            Start = start;
            End = end;
        }
    }
}
