using System;
using System.Collections.Generic;
using Styx.WoWInternals.WoWObjects;

namespace Styx.CommonBot
{
    /// <summary>
    /// A filter for the target list. This filter adds all potential targets to the outgoing units.
    /// </summary>
    /// <param name="incomingUnits">A list of incoming units. This is potential units that can be added to outgoing units.</param>
    /// <param name="outgoingUnits">A set of final units in the target list. Add units you want in the target list here.</param>
    public delegate void IncludeTargetsFilterDelegate(List<WoWObject> incomingUnits, HashSet<WoWObject> outgoingUnits);
}
