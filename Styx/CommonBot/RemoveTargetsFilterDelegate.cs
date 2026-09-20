using System;
using System.Collections.Generic;
using Styx.WoWInternals.WoWObjects;

namespace Styx.CommonBot
{
    /// <summary>
    /// A filter for the target list. This filter should remove invalid units from the list. This is called before the include targets filter.
    /// </summary>
    /// <param name="units"></param>
    public delegate void RemoveTargetsFilterDelegate(List<WoWObject> units);
}
