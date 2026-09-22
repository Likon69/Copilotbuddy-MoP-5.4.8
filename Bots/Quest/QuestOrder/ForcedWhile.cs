using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Media;
using Bots.Quest.Actions;
using Bots.Quest.Resources;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Profiles.Quest.Order;
using Styx.Helpers;
using Styx.TreeSharp;

namespace Bots.Quest.QuestOrder
{
	public class ForcedWhile : ForcedBehavior
	{
		private class ForcedWhileExecutor : Composite
		{
			public ForcedWhileExecutor(WhileNode node)
			{
				_whileNode = node;
			}

			public bool IsDone { get; private set; }

			private readonly WhileNode _whileNode;
			public ForcedBehaviorExecutor Executor { get; private set; }
			private bool _evaluated;

			protected override IEnumerable<RunStatus> Execute(object context)
			{
				if (!_evaluated)
				{
					bool condition;
					try
					{
						condition = _whileNode.Condition();
					}
					catch (Exception ex)
					{
						if (ex is ThreadAbortException)
							throw;

						Logging.Write(Colors.Red, QuestBotResources.Unable_to_evaluate_compile_condition_in_If_tag__Please_check_your_profile_);
						Logging.Write(Colors.Red, QuestBotResources.Honorbuddy_stopped);
						Logging.WriteException(ex);
                        TreeRoot.Stop(QuestBotResources.Unable_to_evaluate_compile_condition_in_If_tag__Please_check_your_profile_);
						yield break; // TreeRoot.Stop should throw ThreadAbortException
					}

					if (!condition)
					{
						IsDone = true;
						yield return RunStatus.Success;
						yield break;
					}

					_evaluated = true;

					var order = new QuestOrder(new OrderNodeCollection(_whileNode.Body))
					            	{
					            		IgnoreCheckpoints = QuestState.Instance.Order.IgnoreCheckpoints
					            	};

					order.UpdateNodes();
					Executor = new ForcedBehaviorExecutor(order);
				}

				if (Executor.Order.Nodes.Count <= 0)
				{
					_evaluated = false;
					yield return RunStatus.Success;
					yield break;
				}

				Executor.Start(context);
				while (Executor.Tick(context) == RunStatus.Running)
					yield return RunStatus.Running;
				Executor.Stop(context);

				yield return Executor.LastStatus ?? RunStatus.Failure;
				yield break; // so Contributor002 doesn't enrage even though it's completely useless
			}
		}

		public ForcedWhile(WhileNode node)
		{
			if (node == null)
				throw new ArgumentNullException("node");

			WhileNode = node;
		}

		public WhileNode WhileNode { get; private set; }

		private ForcedWhileExecutor _whileExecutor;
		private ForcedBehavior CurrentBehavior
		{
			get
			{
				return _whileExecutor != null && _whileExecutor.Executor != null
					? _whileExecutor.Executor.Order.CurrentBehavior
					: null;
			}
		}
		protected override Composite CreateBehavior()
		{
			return _whileExecutor ?? (_whileExecutor = new ForcedWhileExecutor(WhileNode));
		}

		public override bool IsDone { get { return _whileExecutor != null && _whileExecutor.IsDone; } }

		public override void OnFinished()
		{
			base.OnFinished();
			if (CurrentBehavior != null)
				CurrentBehavior.OnFinished();
		}

		[Obsolete("Use OnFinished instead.")]
		public override void Dispose()
		{
			base.Dispose();
			if (CurrentBehavior != null)
				CurrentBehavior.Dispose();
		}
	}
}
