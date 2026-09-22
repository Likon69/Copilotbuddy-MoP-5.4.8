using Styx.WoWInternals.WoWObjects;

namespace Styx.Pathing.Avoidance
{
    public class AvoidObject : Avoid
    {
        private readonly AvoidObjectInfo _avoidObjectInfo;
        private WoWPoint _location;

        public AvoidObject(AvoidObjectInfo avoidInfo, WoWObject obj)
            : base(avoidInfo)
        {
            _avoidObjectInfo = avoidInfo;
            Object = obj;
        }

        public WoWObject Object { get; private set; }
        public override WoWPoint Location { get { return _location; } }

        public override bool IsValid
        {
            get
            {
                return Object != null && Object.IsValid && AvoidInfo.CanRun(Object) &&
                       _avoidObjectInfo.ObjectSelector(Object);
            }
        }

        internal override void UpdateLocation()
        {
            _location = Object != null && Object.IsValid
                ? _avoidObjectInfo.LocationSelector(Object)
                : WoWPoint.Zero;
        }
    }
}
