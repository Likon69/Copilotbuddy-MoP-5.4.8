using System;
using Styx.TreeSharp;
using Styx.WoWInternals.WoWObjects;

namespace Styx.Pathing.Avoidance
{
    public class AvoidObjectInfo : AvoidInfo
    {
        public AvoidObjectInfo(CanRunDecoratorDelegate condition, Predicate<WoWObject> objectSelector,
            Func<float> radiusSelector, Func<WoWPoint> leashPointSelector = null, float leashRadius = 40f,
            Func<WoWObject, WoWPoint> locationSelector = null, bool isBlocking = true)
            : base(condition, radiusSelector, leashPointSelector, leashRadius, isBlocking)
        {
            ObjectSelector = objectSelector;
            LocationSelector = locationSelector ?? (obj => obj.Location);
        }

        public Predicate<WoWObject> ObjectSelector { get; private set; }
        public Func<WoWObject, WoWPoint> LocationSelector { get; private set; }
    }
}
