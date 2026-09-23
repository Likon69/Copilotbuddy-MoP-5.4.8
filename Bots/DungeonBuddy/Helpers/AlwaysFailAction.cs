using System;
using Styx.TreeSharp;

namespace Bots.DungeonBuddy.Helpers;

public class AlwaysFailAction<T> : Action<T>
{
	public AlwaysFailAction(System.Action<T> failRunner)
		: base(failRunner)
	{
	}

	protected override RunStatus Run(object context)
	{
		base.Run(context);
		return RunStatus.Failure;
	}
}
