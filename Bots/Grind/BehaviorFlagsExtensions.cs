using System;

namespace Bots.Grind
{
    public static class BehaviorFlagsExtensions
    {
        public static bool HasAny(this BehaviorFlags flags, BehaviorFlags flag)
        {
            return (flags & flag) != (BehaviorFlags)0;
        }

        public static bool HasAll(this BehaviorFlags flags, BehaviorFlags flag)
        {
            return (flags & flag) == flag;
        }
    }
}
