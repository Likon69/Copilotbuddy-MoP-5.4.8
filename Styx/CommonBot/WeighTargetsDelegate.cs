using System;
using System.Collections.Generic;

namespace Styx.CommonBot
{
    /// <summary>
    /// Allows a registered callee to set the weights on the specified units.
    /// </summary>
    /// <param name="units"></param>
    public delegate void WeighTargetsDelegate(List<Targeting.TargetPriority> units);
}
