using System;
using System.Collections.Generic;
using Styx.TreeSharp;

namespace Styx.Pathing.Avoidance
{
    public class AvoidLocationInfo : AvoidInfo
    {
        private static readonly object[] OneItemCollection = new object[1];

        public static readonly Func<IEnumerable<object>> DefaultCollectionSelector = () => OneItemCollection;

        public AvoidLocationInfo(CanRunDecoratorDelegate condition, Func<object, WoWPoint> locationSelector,
            Func<float> radiusSelector, Func<WoWPoint> leashPointSelector = null, float leashRadius = 40f,
            Func<IEnumerable<object>> collecionSelection = null, bool isBlocking = true)
            : base(condition, radiusSelector, leashPointSelector, leashRadius, isBlocking)
        {
            LocationSelector = locationSelector;
            CollectionSelector = collecionSelection ?? DefaultCollectionSelector;
        }

        public Func<object, WoWPoint> LocationSelector { get; private set; }
        public Func<IEnumerable<object>> CollectionSelector { get; private set; }
    }
}
