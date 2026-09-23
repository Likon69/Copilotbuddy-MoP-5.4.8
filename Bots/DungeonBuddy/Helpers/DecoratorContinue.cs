using System;
using Styx.TreeSharp;

namespace Bots.DungeonBuddy.Helpers;

public class DecoratorContinue<T> : DecoratorContinue
{
	private readonly Predicate<T> _predicate;

	public DecoratorContinue(Predicate<T> canRun, Composite decorated)
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
