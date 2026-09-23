using System;
using Styx;
using Styx.TreeSharp;

namespace Bots.DungeonBuddy.Avoidance;

public abstract class AvoidInfo
{
	public CanRunDecoratorDelegate Condition { get; private set; }

	public Func<WoWPoint> LeashPointSelector { get; private set; }

	public float LeashRadius { get; private set; }

	public bool IgnoreIfBlocking { get; private set; }

	protected AvoidInfo(CanRunDecoratorDelegate condition, Func<WoWPoint> leashPointSelector, float leashRadius, bool ignoreIfBlocking)
	{
		Condition = condition;
		LeashPointSelector = leashPointSelector;
		LeashRadius = leashRadius;
		IgnoreIfBlocking = ignoreIfBlocking;
	}

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
}
