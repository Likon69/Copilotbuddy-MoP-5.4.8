using System;
using Styx.TreeSharp;

namespace Bots.DungeonBuddy.Helpers;

public class Decorator<T> : Decorator
{
	private readonly Predicate<T> _predicate;

	public Decorator(Predicate<T> canRun, Composite decorated)
		: base(decorated)
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
