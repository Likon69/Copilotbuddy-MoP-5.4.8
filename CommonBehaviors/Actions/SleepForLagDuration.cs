using System;
using Styx;

namespace CommonBehaviors.Actions
{
    public class SleepForLagDuration : Styx.TreeSharp.Sleep
    {
        public SleepForLagDuration()
            : base(context => TimeSpan.FromMilliseconds(StyxWoW.WoWClient.Latency + 100U))
        {
        }
    }
}
