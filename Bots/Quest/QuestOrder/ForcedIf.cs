using System;
using System.Drawing;
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
	public class ForcedIf : ForcedBehavior
	{
		public ForcedIf(IfNode node)
		{
			if (node == null)
				throw new ArgumentNullException("node");

			IfNode = node;
		}

		public IfNode IfNode { get; private set; }

		private QuestOrder _order;
		private Composite _behavior;
		private ForcedBehavior CurrentBehavior { get { return _order == null ? null : _order.CurrentBehavior; } }
		protected override Composite CreateBehavior()
		{
			return _behavior ?? (_behavior = new ForcedBehaviorExecutor(_order));
		}

		public override void OnStart()
		{
            try
            {
                if (IfNode.Condition())
                {
                    _order = new QuestOrder(new OrderNodeCollection(IfNode.Body));
                }
                else
                {
                    OrderNodeCollection col;
                    if (EvaluateElseIfs(out col))
                    {
                        _order = new QuestOrder(new OrderNodeCollection(col));
                    }
                    else if (IfNode.Else != null)
                    {
                        _order = new QuestOrder(new OrderNodeCollection(IfNode.Else.Body));
                    }
                }
            }
            catch(Exception ex)
            {
                Logging.Write(Colors.Red, QuestBotResources.Unable_to_evaluate_compile_condition_in_If_tag__Please_check_your_profile_);
                Logging.Write(Colors.Red, QuestBotResources.Honorbuddy_stopped);
                Logging.WriteException(ex);
                TreeRoot.Stop(QuestBotResources.Unable_to_evaluate_compile_condition_in_If_tag__Please_check_your_profile_);
            }

		    if (_order == null)
				return;

			_order.IgnoreCheckpoints = QuestState.Instance.Order.IgnoreCheckpoints;
			_order.UpdateNodes();
		}

		private bool EvaluateElseIfs(out OrderNodeCollection collection)
		{
			foreach (ElseIf elseIf in IfNode.ElseIfs)
			{
				if (elseIf.Condition())
				{
					collection = elseIf.Body;
					return true;
				}
			}

			collection = null;
			return false;
		}

		public override bool IsDone { get { return _order == null || _order.Nodes == null || _order.Nodes.Count <= 0; } }

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
