using System;
using System.Collections.Generic;
using Styx;
using Styx.TreeSharp;

namespace Bots.DungeonBuddy.Avoidance;

public class AvoidLocationInfo : AvoidInfo
{
	public readonly Func<object, float> RadiusSelector;

	private static readonly object[] _defaultContent;

	public static readonly Func<IEnumerable<object>> DefaultCollectionSelector;

	public Func<object, WoWPoint> LocationSelector { get; private set; }

	public Func<IEnumerable<object>> CollectionSelector { get; private set; }

	public AvoidLocationInfo(CanRunDecoratorDelegate condition, Func<object, WoWPoint> locationSelector, Func<object, float> radiusSelector, Func<WoWPoint> leashPointSelector = null, float leashRadius = 40f, Func<IEnumerable<object>> collecionSelection = null, bool ignoreIfBlocking = false)
		: base(condition, leashPointSelector, leashRadius, ignoreIfBlocking)
	{
		LocationSelector = locationSelector;
		RadiusSelector = radiusSelector;
		CollectionSelector = collecionSelection ?? DefaultCollectionSelector;
	}

	static AvoidLocationInfo()
	{
		_defaultContent = new object[1];
		DefaultCollectionSelector = () => _defaultContent;
	}
}
