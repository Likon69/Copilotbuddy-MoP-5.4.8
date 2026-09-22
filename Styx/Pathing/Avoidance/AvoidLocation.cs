namespace Styx.Pathing.Avoidance
{
    public class AvoidLocation : Avoid
    {
        private WoWPoint _location;
        private readonly AvoidLocationInfo _avoidLocationInfo;

        public AvoidLocation(AvoidLocationInfo avoidInfo, object content)
            : base(avoidInfo)
        {
            _avoidLocationInfo = avoidInfo;
            Content = content;
        }

        public override WoWPoint Location { get { return _location; } }
        public object Content { get; private set; }

        internal override void UpdateLocation()
        {
            _location = IsValid ? _avoidLocationInfo.LocationSelector(Content) : WoWPoint.Zero;
        }
    }
}
