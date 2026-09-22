using System;
using System.Text;
using Bots.Quest.Objectives;
using Bots.Quest.Resources;
using Styx;
using Styx.CommonBot;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWCache;
using Styx.WoWInternals.WoWObjects;

namespace Bots.Quest.QuestOrder
{
	public class ForcedQuestObjective : ForcedBehavior
	{
		public ForcedQuestObjective(QuestObjective objective)
		{
			if (objective == null)
				throw new ArgumentNullException("objective");

			Objective = objective;
		}

		public QuestObjective Objective { get; private set; }

		protected override Composite CreateBehavior()
		{
			return Objective.CreateBranch();
		}

        public override void OnFinished()
        {
			if (Objective != null)
			{
				Objective.Dispose();
				Objective = null;
			}
        }

		public override bool IsDone { get { return Objective.IsCompleted; } }

		public override void OnStart()
		{
			if (Objective != null)
				Objective.OnStart();
			TreeRoot.GoalText = GetFriendlyObjectiveText(Objective);
		}

		public override string ToString()
		{
			return string.Format(QuestBotResources.ForcedQuestObjective_ToString__ForcedQuestObjective_Objective___0__, Objective);
		}

		private static string GetFriendlyObjectiveText(QuestObjective objective)
		{
			if (objective is CollectItemObjective)
			{
				var obj = (CollectItemObjective)objective;
				ItemInfo item = ItemInfo.FromId((uint)obj.Objective.ID);
				if (item != null)
					return string.Format(QuestBotResources.ForcedQuestObjective_GetFriendlyObjectiveText_Goal__Collect__0__x__1_, item.Name, obj.Objective.Count);

				return string.Format(QuestBotResources.ForcedQuestObjective_GetFriendlyObjectiveText_Goal__Collect__0__of_item_with_ID__1_, obj.Objective.Count, obj.Objective.ID);
			}
			if (objective is GrindObjective)
			{
				var obj = (GrindObjective)objective;
				WoWCache.InfoBlock block = StyxWoW.Cache[CacheDb.Creature].GetInfoBlockById((uint)obj.Objective.ID);
				if (block != null)
				{
					WoWCache.CreatureCacheEntry inf = block.Creature;
                    return string.Format(QuestBotResources.ForcedQuestObjective_GetFriendlyObjectiveText_Goal__Kill__0__x__1_, StyxWoW.Memory.ReadString(inf.Name[0].PtrStr, Encoding.UTF8), obj.Objective.Count);
				}

				return string.Format(QuestBotResources.ForcedQuestObjective_GetFriendlyObjectiveText_Goal__Kill_mob_with_ID__0___1__times, obj.GetOverrideInfo() != null ? obj.GetOverrideInfo().MobID : obj.Objective.ID, obj.Objective.Count);
			}
			if (objective is UseGameObjectObjective)
			{
				var obj = (UseGameObjectObjective)objective;
				WoWCache.InfoBlock block = StyxWoW.Cache[CacheDb.GameObject].GetInfoBlockById((uint)obj.Objective.ID);
				if (block != null)
				{
					WoWCache.GameObjectCacheEntry inf = block.GameObject;
					return string.Format(QuestBotResources.ForcedQuestObjective_GetFriendlyObjectiveText_Goal__Use__0__x__1_, StyxWoW.Memory.ReadString(inf.NamePtrs[0], Encoding.UTF8), obj.Objective.Count);
				}

				return string.Format(QuestBotResources.ForcedQuestObjective_GetFriendlyObjectiveText_Goal__Use_object_with_ID__0___1__times, obj.Objective.ID, obj.Objective.Count);
			}

			return "";
		}
	}
}
