using System;
using Styx.TreeSharp;

namespace Bots.Quest.QuestOrder
{
	public abstract class ForcedBehavior : IDisposable
	{
		private Composite _branch;
		public Composite Branch { get { return _branch ?? (_branch = CreateBehavior()); } }
		protected abstract Composite CreateBehavior();

		public abstract bool IsDone { get; }

		/// <summary>
		/// Called right after this behavior has been set as the current behavior.
		/// </summary>
		public virtual void OnStart()
		{
		}

		/// <summary>
		/// Called when the behavior tree is ticked.
		/// </summary>
		public virtual void OnTick()
		{
		}

		/// <summary>
		/// Called right before this behavior is removed as the current behavior.
		/// </summary>
		[Obsolete("Use OnFinished instead.")]
		public virtual void Dispose()
		{
		}

		public virtual void OnFinished()
		{
		}
	}
}
