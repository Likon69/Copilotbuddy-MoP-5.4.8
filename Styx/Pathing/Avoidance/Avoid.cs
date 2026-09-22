using Tripper.Tools.Math;

namespace Styx.Pathing.Avoidance
{
    public abstract class Avoid
    {
        protected Avoid(AvoidInfo avoidInfo)
        {
            AvoidInfo = avoidInfo;
        }

        public AvoidInfo AvoidInfo { get; private set; }
        public float Radius { get { return AvoidInfo.RadiusSelector(); } }
        public WoWPoint LeashPoint { get { return AvoidInfo.LeashPointSelector == null ? WoWPoint.Zero : AvoidInfo.LeashPointSelector(); } }
        public float LeashRadius { get { return AvoidInfo.LeashRadius; } }
        public bool IsBlocking { get { return AvoidInfo.IsBlocking; } }
        public abstract WoWPoint Location { get; }
        public float RadiusSqr { get { return Radius * Radius; } }
        public float LeashRadiusSqr { get { return LeashRadius * LeashRadius; } }
        public virtual bool IsValid { get { return AvoidInfo.CanRun(null); } }

        public bool IsPointInAvoid(Vector3 point)
        {
            float radius = Radius + 0.25f;
            return Location != WoWPoint.Zero && Location.Distance2DSqr(point) <= radius * radius;
        }

        internal abstract void UpdateLocation();
    }
}
