using Bots.Quest.Resources;
using CommonBehaviors.Actions;
using Styx.TreeSharp;

namespace Bots.Quest.QuestOrder
{
	public class ForcedNothing : ForcedBehavior
	{
		protected override Composite CreateBehavior()
		{
			return new ActionAlwaysSucceed();
		}

		public override bool IsDone { get { return true; } }

		public override string ToString()
		{
			return QuestBotResources.ForcedNothing_ToString__ForcedNothing_;
		}
	}
}
