using System;
using Styx.TreeSharp;

namespace Bots.DungeonBuddy.Helpers;

public class WaitContinue<T> : WaitContinue
{
	private readonly Predicate<T> _predicate;

	public WaitContinue(int timeoutSeconds, Predicate<T> canRun, Composite child)
		: base(timeoutSeconds, child)
	{
		_predicate = canRun;
	}

	public WaitContinue(TimeSpan timeout, Predicate<T> canRun, Composite child)
		: base(timeout, child)
	{
		_predicate = canRun;
	}

	public WaitContinue(WaitGetTimeoutDelegate timeoutRetriever, Predicate<T> canRun, Composite child)
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
