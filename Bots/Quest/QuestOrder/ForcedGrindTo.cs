using System;
using Bots.Grind;
using Bots.Quest.Resources;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.POI;
using Styx.CommonBot.Profiles.Quest.Order;
using Styx.Helpers;

using Styx.TreeSharp;
using Styx.WoWInternals;

namespace Bots.Quest.QuestOrder
{
	public class ForcedGrindTo : ForcedBehavior
	{
        public ForcedGrindTo(GrindToNode node)
        {
			if (node == null)
				throw new ArgumentNullException("node");

			Node = node;
        }

		public GrindToNode Node { get; private set; }

		protected override Composite CreateBehavior()
		{
		    return LevelBot.CreateRoamBehavior(Node.NavType);
		}

		public override bool IsDone
		{
			get
			{
                if (Node.Condition != null)
                    return Node.Condition();

				return StyxWoW.Me.LevelFraction >= Node.Level;
			}
		}

        public override void OnStart()
        {
			if (QuestState.Instance.CurrentGrindArea == null)
			{
				//Logging.Write(QuestBotResources.ForcedGrindTo_OnStart_Reached_GrindTo__level___0___without_a_current_grind_area__Can_t_continue_, Node.Level);
                TreeRoot.Stop(string.Format(QuestBotResources.ForcedGrindTo_OnStart_Reached_GrindTo__level___0___without_a_current_grind_area__Can_t_continue_, Node.Level));
				return;
			}

			StyxWoW.AreaManager.SetArea(QuestState.Instance.CurrentGrindArea);
			TreeRoot.GoalText = GetGoalText();
        }

		private string GetGoalText()
		{
			//Node.Level != -1 ? string.Format(QuestBotResources.ForcedGrindTo_OnStart_Goal__Grind_to_level__0_, Node.Level) : "Grinding";
			if (!string.IsNullOrEmpty(Node.GoalText))
				return Node.GoalText;

			if (Node.Level == -1)
				return QuestBotResources.ForcedGrindTo_GetGoalText_Grinding;

			return string.Format(QuestBotResources.ForcedGrindTo_GetGoalText_Grinding_to_level__0_, Node.Level);
		}

        public override void OnFinished()
        {
			StyxWoW.AreaManager.SetArea(null);
			BotPoi.Clear("Finished with grinding");
        }

		public override string ToString()
		{
			return string.Format(QuestBotResources.ForcedGrindTo_ToString__ForcedGrindTo_Level___0__, Node.Level);
		}
	}
}
