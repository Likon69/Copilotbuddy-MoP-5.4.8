using System;
using System.Collections.Generic;
using System.Text;
using Bots.Quest.Objectives;
using Styx;
using Styx.CommonBot.Frames;
using Styx.WoWInternals;

namespace Bots.Quest
{
	public static class QuestManager
	{
        public static readonly GossipFrame GossipFrame = new GossipFrame();
        public static readonly QuestFrame QuestFrame = new QuestFrame();

		public static readonly List<QuestObjective> Objectives = new List<QuestObjective>();
		public static readonly List<PlayerQuest> Quests = new List<PlayerQuest>();
        
		private static readonly Random _rand = new Random();
		private static readonly char[] _charpool = "ABCDEFGHIJKLMNOPQRSTUVXYZabcdefghijklmnopqrstuvxyz".ToCharArray();
		private static string GenerateRandomString(int minLen, int maxLen)
		{
			int len = _rand.Next(minLen, maxLen + 1);
			var str = new StringBuilder(len);
			for (int i = 0; i < len; i++)
				str.Append(_charpool[_rand.Next(0, _charpool.Length)]);

			return str.ToString();
		}

		internal static QuestObjective ConstructQuestObjective(Styx.WoWInternals.Quest.QuestObjective objective, PlayerQuest quest, List<WoWQuestStep> steps, List<QuestObjective> previousObjectives, WoWPoint location, NavType navType)
		{
			switch (objective.Type)
			{
				case Styx.WoWInternals.Quest.QuestObjectiveType.CollectIntermediateItem:
				case Styx.WoWInternals.Quest.QuestObjectiveType.CollectItem:
					return new CollectItemObjective(quest, steps, objective, previousObjectives, location, navType);
				case Styx.WoWInternals.Quest.QuestObjectiveType.KillMob:
					return new GrindObjective(quest, steps, objective, previousObjectives, location, navType);
				case Styx.WoWInternals.Quest.QuestObjectiveType.UseGameObject:
					return new UseGameObjectObjective(quest, steps, objective, previousObjectives, location, navType);
				default:
					return null;
			}
		}

		#region Nested type: QuestObjectiveChangedDelegate

		internal delegate void QuestObjectiveChangedDelegate(QuestObjective objective);

		#endregion

		#region Nested type: QuestsRefreshedDelegate

		internal delegate void QuestsRefreshedDelegate(List<PlayerQuest> quests);

		#endregion
	}
}
