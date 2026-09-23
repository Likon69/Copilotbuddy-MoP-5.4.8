using Styx;
using Tripper.Tools.Math;

namespace Bots.DungeonBuddy.Avoidance;

public abstract class Avoid
{
	public AvoidInfo AvoidInfo { get; private set; }

	public abstract float Radius { get; }

	public WoWPoint LeashPoint
	{
		get
		{
			if (AvoidInfo.LeashPointSelector == null)
			{
				return WoWPoint.Zero;
			}
			return AvoidInfo.LeashPointSelector();
		}
	}

	public float LeashRadius => AvoidInfo.LeashRadius;

	public bool IgnoreIfBlocking => AvoidInfo.IgnoreIfBlocking;

	public abstract WoWPoint Location { get; }

	public float RadiusSqr
	{
		get
		{
			float radius = Radius;
			return radius * radius;
		}
	}

	public float LeashRadiusSqr => LeashRadius * LeashRadius;

	public virtual bool IsValid => AvoidInfo.CanRun(null);

	public bool IsBlocking { get; internal set; }

	protected Avoid(AvoidInfo avoidInfo)
	{
		AvoidInfo = avoidInfo;
	}

	public bool IsPointInAvoid(Vector3 point)
	{
		float num = Radius + 0.5f;
		if (Location != WoWPoint.Zero)
		{
			return Location.Distance2DSqr(point) <= num * num;
		}
		return false;
	}

	internal abstract void UpdateLocationAndRadius();
}
