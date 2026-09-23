using Styx;
using Styx.WoWInternals.WoWObjects;

namespace Bots.DungeonBuddy.Avoidance;

public class AvoidObject : Avoid
{
	private readonly AvoidObjectInfo _avoidObjectInfo;

	private float _radius;

	private WoWPoint _location;

	public WoWObject Object { get; private set; }

	public override float Radius => _radius;

	public override WoWPoint Location => _location;

	public override bool IsValid
	{
		get
		{
			if (Object != null && Object.IsValid && base.AvoidInfo.CanRun(Object))
			{
				return _avoidObjectInfo.ObjectSelector(Object);
			}
			return false;
		}
	}

	public AvoidObject(AvoidObjectInfo avoidInfo, WoWObject obj)
		: base(avoidInfo)
	{
		_avoidObjectInfo = avoidInfo;
		Object = obj;
	}

	internal override void UpdateLocationAndRadius()
	{
		if (Object != null && Object.IsValid)
		{
			_location = _avoidObjectInfo.LocationSelector(Object);
			_radius = _avoidObjectInfo.RadiusSelector(Object);
		}
		else
		{
			_location = WoWPoint.Zero;
			_radius = 0f;
		}
	}
}
