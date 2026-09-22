using System.Collections.Generic;
using System.Text;
using Styx;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWCache;
using Styx.WoWInternals.WoWObjects;

namespace Bots.Quest
{
	public static class QuestDebug
	{
		public static string GetQuestDebugString(PlayerQuest quest)
		{
			if (quest == null)
				return "QUEST IS NULL";

			var str = new StringBuilder();
			str.AppendFormat("Information about quest <{0}>\n", quest.Name);
			str.AppendFormat("  - ID: {0}\n", quest.Id);
			str.AppendFormat("  - Level: {0}\n", quest.Level);
			str.AppendFormat("  - Flags: {0}\n", quest.InternalInfo.Flags);
			str.AppendFormat("  - Objectives:\n");
			List<Styx.WoWInternals.Quest.QuestObjective> objectives = quest.GetObjectives();
			for (int i = 0; i < objectives.Count; i++)
			{
				if (objectives[i].IsEmpty)
					continue;

				str.AppendFormat("    - #{0}:\n", i);
				str.AppendFormat("      - Type: {0}\n", objectives[i].Type);
				str.AppendFormat("      - ID: {0}\n", objectives[i].ID);
				str.AppendFormat("      - Count: {0}\n", objectives[i].Count);
				str.AppendFormat("      - Objective: {0}\n", string.IsNullOrEmpty(objectives[i].Objective) ? "(null)" : objectives[i].Objective);

				switch (objectives[i].Type)
				{
					case Styx.WoWInternals.Quest.QuestObjectiveType.CollectItem:
					case Styx.WoWInternals.Quest.QuestObjectiveType.CollectIntermediateItem:
						ItemInfo itemInfo = ItemInfo.FromId((uint)objectives[i].ID);
						if (itemInfo == null)
							str.AppendFormat("      - ITEM NOT CACHED\n");
						else
						{
							str.AppendFormat("      - Item Name: {0}\n", itemInfo.Name);
						}
						break;
					case Styx.WoWInternals.Quest.QuestObjectiveType.KillMob:
						var creatureInfoBlock = StyxWoW.Cache[CacheDb.Creature].GetInfoBlockById((uint)objectives[i].ID);
						if (creatureInfoBlock == null)
							str.AppendFormat("      - CREATURE NOT CACHED\n");
						else
						{
							WoWCache.CreatureCacheEntry creature = creatureInfoBlock.Creature;
						    str.AppendFormat("      - Creature Name: {0}\n",
						        StyxWoW.Memory.ReadString(creature.Name[0].PtrStr, Encoding.UTF8));
						}
						break;
					case Styx.WoWInternals.Quest.QuestObjectiveType.UseGameObject:
                        var gameObjectInfoBlock = StyxWoW.Cache[CacheDb.GameObject].GetInfoBlockById((uint)objectives[i].ID);
						if (gameObjectInfoBlock == null)
							str.AppendFormat("      - GAME OBJECT NOT CACHED\n");
						else
						{
							WoWCache.GameObjectCacheEntry gameObj = gameObjectInfoBlock.GameObject;
							str.AppendFormat("      - Game Object Name: {0}\n", StyxWoW.Memory.ReadString(gameObj.NamePtrs[0], Encoding.ASCII));
						}
						break;
				}
			}
			str.AppendFormat("  - Completion info:\n");
			WoWQuestPOIInfo poiInfo = quest.GetQuestPOIInfo();
			str.AppendFormat("    - Steps:\n");
			WoWQuestStep[] steps = poiInfo.Steps.Steps;
			for (int i = 0; i < steps.Length; i++)
			{
				str.AppendFormat("      - #{0}:\n", i);
				str.AppendFormat("        - Objective: #{0}\n", steps[i].PoiObjectiveIndex);
   			}

			return str.ToString();
		}
	}
}
