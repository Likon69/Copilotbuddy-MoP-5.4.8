using Styx;

namespace Bots.DungeonBuddy.Avoidance;

public class AvoidLocation : Avoid
{
	private float _radius;

	private WoWPoint _location;

	private readonly AvoidLocationInfo _avoidLocationInfo;

	public override float Radius => _radius;

	public override WoWPoint Location => _location;

	public object Content { get; private set; }

	public AvoidLocation(AvoidLocationInfo avoidInfo, object content)
		: base(avoidInfo)
	{
		_avoidLocationInfo = avoidInfo;
		Content = content;
	}

	internal override void UpdateLocationAndRadius()
	{
		if (IsValid)
		{
			_location = _avoidLocationInfo.LocationSelector(Content);
			_radius = _avoidLocationInfo.RadiusSelector(Content);
		}
		else
		{
			_location = WoWPoint.Zero;
			_radius = 0f;
		}
	}
}
