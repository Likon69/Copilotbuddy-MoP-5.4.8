using System;
using System.Collections.Generic;
using Styx;
using Styx.CommonBot.Profiles.Quest.Order;

namespace Bots.Quest.QuestOrder
{
	public class QuestOrder
	{
        public static QuestOrder Instance { get; private set; }

		public QuestOrder()
		{
		    Instance = this;
		}

		public QuestOrder(OrderNodeCollection order)
		{
			Nodes = order;
		}

		public event EventHandler<EventArgs> OnNoMoreNodes;

		public OrderNodeCollection Nodes { get; set; }

		public bool IgnoreCheckpoints { get { return Nodes.IgnoreCheckpoints; } set { Nodes.IgnoreCheckpoints = value; } }

		public ForcedBehavior CurrentBehavior { get; set; }

		public OrderNode CurrentNode
		{
			get
			{
				if (Nodes == null || Nodes.Count <= 0)
					return null;

				return Nodes[0];
			}
		}

        public OrderNode NextNode
        {
            get
            {
                if (Nodes == null || Nodes.Count <= 1)
                    return null;

                return Nodes[1];
            }
        }

		public void Advance()
		{
			Advance(1);
		}

		public void Advance(int times)
		{
			if (Nodes == null || Nodes.Count <= 0 || times <= 0)
				return;

			Nodes.RemoveRange(0, Math.Min(Nodes.Count, times));
			if (Nodes.Count <= 0 && OnNoMoreNodes != null)
				OnNoMoreNodes(this, EventArgs.Empty);
		}

		/// <summary>
		/// First skips to the current checkpoint based on the characters level (if IgnoreCheckpoints isn't true), then removes all already completed nodes.
		/// </summary>
		public void UpdateNodes()
		{
			if (Nodes == null)
				return;

			if (!IgnoreCheckpoints)
				SkipToCheckpoint(StyxWoW.Me.LevelFraction);

			RemoveCompletedNodes(new HashSet<uint>(StyxWoW.Me.QuestLog.GetCompletedQuests()));
		}

		public void SkipToCheckpoint(float level)
		{
			if (Nodes == null)
				return;

			int count = 0;
			for (int i = 0; i < Nodes.Count; i++)
			{
				if (!(Nodes[i] is CheckpointNode))
					continue;

				var node = (CheckpointNode)Nodes[i];
				if (node.Level > level)
					break;

				count = i + 1;
			}

			Advance(count);
		}

		public void RemoveCompletedNodes(HashSet<uint> completedQuests)
		{
			for (int i = Nodes.Count - 1; i >= 0; i--)
			{
				if (ShouldRemove(Nodes[i], completedQuests))
					Nodes.RemoveAt(i);
			}
		}

		private bool ShouldRemove(OrderNode node, HashSet<uint> completedQuests)
		{
			switch (node.Type)
			{
				default:
					return false;
				case OrderNodeType.AbandonQuest:
					var abandon = (AbandonQuestNode)node;
					return completedQuests.Contains(abandon.QuestId);
				case OrderNodeType.MoveTo:
					var moveTo = (MoveToNode)node;
					if (moveTo.QuestId != 0)
						return completedQuests.Contains(moveTo.QuestId);
					return false;
				case OrderNodeType.UseItem:
					var useItem = (UseItemNode)node;
					return completedQuests.Contains(useItem.QuestId);
				case OrderNodeType.PickUp:
					var pickUp = (PickUpNode)node;
					return completedQuests.Contains(pickUp.QuestId);
				case OrderNodeType.TurnIn:
					var turnIn = (TurnInNode)node;
					return completedQuests.Contains(turnIn.QuestId);
				case OrderNodeType.Objective:
					var objective = (ObjectiveNode)node;
					return completedQuests.Contains(objective.QuestId);
			}
		}
	}
}
