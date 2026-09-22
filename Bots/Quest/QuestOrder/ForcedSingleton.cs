using Bots.Quest.Resources;
using CommonBehaviors.Actions;
using Styx.TreeSharp;
using Action = System.Action;

namespace Bots.Quest.QuestOrder
{
	public class ForcedSingleton : ForcedBehavior
	{
		public ForcedSingleton(Action action)
		{
			Action = action;
		}

		public Action Action { get; private set; }

		protected override Composite CreateBehavior()
		{
			return new ActionAlwaysSucceed();
		}

		public override bool IsDone { get { return true; } }

		public override void OnStart()
		{
			Action();
		}

		public override string ToString()
		{
			return string.Format(QuestBotResources.ForcedSingleton_ToString__ForcedSingleton_);
		}
	}
}
