using System;
using System.Collections.Generic;
using Styx.TreeSharp;

namespace Bots.DungeonBuddy.Behaviors;

internal class ForeachContext<T> : Composite
{
	public Composite Behavior { get; private set; }

	public Func<object, IEnumerable<T>> ContextsRetriever { get; private set; }

	public ForeachContext(Composite behavior, Func<object, IEnumerable<T>> contextsRetriever)
	{
		Behavior = behavior;
		ContextsRetriever = contextsRetriever;
	}

	protected override IEnumerable<RunStatus> Execute(object context)
	{
		foreach (T item in ContextsRetriever(context))
		{
			Behavior.Start(item);
			while (Behavior.Tick(item) == RunStatus.Running)
			{
				yield return RunStatus.Running;
			}
			Behavior.Stop(item);
			if (Behavior.LastStatus == RunStatus.Success)
			{
				yield return RunStatus.Success;
				yield break;
			}
		}
		yield return RunStatus.Failure;
	}
}
