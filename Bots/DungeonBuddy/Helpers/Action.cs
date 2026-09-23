using System;
using Styx.TreeSharp;

namespace Bots.DungeonBuddy.Helpers;

public class Action<T> : Styx.TreeSharp.Action
{
	protected readonly Func<T, RunStatus> ActionRunner;

	protected readonly System.Action<T> SuccessRunner;

	public Action(Func<T, RunStatus> actionRunner)
	{
		ActionRunner = actionRunner;
	}

	public Action(System.Action<T> successRunner)
	{
		SuccessRunner = successRunner;
	}

	protected override RunStatus Run(object context)
	{
		if (!(context is T))
		{
			return RunStatus.Failure;
		}
		if (ActionRunner != null)
		{
			return ActionRunner((T)context);
		}
		if (SuccessRunner != null)
		{
			SuccessRunner((T)context);
			return RunStatus.Success;
		}
		return RunStatus.Failure;
	}
}
