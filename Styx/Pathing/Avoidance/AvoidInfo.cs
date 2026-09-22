using System;
using Styx.TreeSharp;

namespace Styx.Pathing.Avoidance
{
    public abstract class AvoidInfo
    {
        protected AvoidInfo(CanRunDecoratorDelegate condition, Func<float> radiusSelector,
            Func<WoWPoint> leashPointSelector, float leashRadius, bool isBlocking)
        {
            Condition = condition;
            RadiusSelector = radiusSelector;
            LeashPointSelector = leashPointSelector;
            LeashRadius = leashRadius;
            IsBlocking = isBlocking;
        }

        public CanRunDecoratorDelegate Condition { get; private set; }
        public Func<float> RadiusSelector { get; private set; }
        public Func<WoWPoint> LeashPointSelector { get; private set; }
        public float LeashRadius { get; private set; }
        public bool IsBlocking { get; private set; }

        public bool CanRun(object ctx)
        {
            try
            {
                return Condition(ctx);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public float RadiusSqr
        {
            get
            {
                float radius = RadiusSelector();
                return radius * radius;
            }
        }
    }
}
