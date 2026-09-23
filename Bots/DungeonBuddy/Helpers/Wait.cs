using System;
using Styx.TreeSharp;

namespace Bots.DungeonBuddy.Helpers;

public class Wait<T> : Wait
{
	private readonly Predicate<T> _predicate;

	public Wait(int timeoutSeconds, Predicate<T> canRun, Composite child)
		: base(timeoutSeconds, child)
	{
		_predicate = canRun;
	}

	public Wait(TimeSpan timeout, Predicate<T> canRun, Composite child)
		: base(timeout, child)
	{
		_predicate = canRun;
	}

	public Wait(WaitGetTimeoutDelegate timeoutRetriever, Predicate<T> canRun, Composite child)
		: base(timeoutRetriever, child)
	{
		_predicate = canRun;
	}

	protected override bool CanRun(object context)
	{
		if (context is T)
		{
			return _predicate((T)context);
		}
		return false;
	}
}
