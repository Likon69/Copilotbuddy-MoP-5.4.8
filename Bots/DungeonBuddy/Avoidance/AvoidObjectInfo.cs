using System;
using Styx;
using Styx.TreeSharp;
using Styx.WoWInternals.WoWObjects;

namespace Bots.DungeonBuddy.Avoidance;

public class AvoidObjectInfo : AvoidInfo
{
	public readonly Func<WoWObject, float> RadiusSelector;

	public Predicate<WoWObject> ObjectSelector { get; private set; }

	public Func<WoWObject, WoWPoint> LocationSelector { get; private set; }

	public AvoidObjectInfo(CanRunDecoratorDelegate condition, Predicate<WoWObject> objectSelector, Func<WoWObject, float> radiusSelector, Func<WoWPoint> leashPointSelector = null, float leashRadius = 40f, Func<WoWObject, WoWPoint> locationSelector = null, bool ignoreIfBlocking = false)
		: base(condition, leashPointSelector, leashRadius, ignoreIfBlocking)
	{
		ObjectSelector = objectSelector;
		RadiusSelector = radiusSelector;
		LocationSelector = locationSelector ?? ((WoWObject obj) => obj.Location);
	}
}
