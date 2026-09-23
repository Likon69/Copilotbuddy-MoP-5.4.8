using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using Bots.DungeonBuddy.Enums;
using Bots.DungeonBuddy.Helpers;
using Buddy.Coroutines;
using Styx;
using Styx.CommonBot;
using Styx.Helpers;
using Styx.WoWInternals;

namespace Bots.DungeonBuddy;

/// <summary>
/// Handles synchronizing the in-game LFG/LFR queue with the dungeons/raids/scenarios selected in <see cref="DungeonBuddySettings"/>.
/// </summary>
internal static class LfgQueue
{
	private enum RandomQueueKind
	{
		Dungeon,
		Raid,
		Scenario
	}

	private class LfgQueueException : Exception
	{
		public LfgQueueException(string message)
			: base(message)
		{
		}
	}

	// number of bosses required to be killed for a given lfrId to be considered fully completed for the week.
	private static readonly Dictionary<uint, uint> LfrBossCounts = new Dictionary<uint, uint>
	{
		{ 527u, 3u },
		{ 528u, 3u },
		{ 529u, 3u },
		{ 530u, 3u },
		{ 526u, 4u },
		{ 610u, 3u },
		{ 611u, 3u },
		{ 612u, 3u },
		{ 613u, 3u },
		{ 716u, 4u },
		{ 717u, 4u },
		{ 724u, 3u },
		{ 725u, 3u }
	};

	private static readonly Dictionary<LfgCategory, Func<List<LfgDungeons>>> DesiredDungeonListProviders = new Dictionary<LfgCategory, Func<List<LfgDungeons>>>
	{
		{ LfgCategory.LookingForDungeon, GetDesiredDungeons },
		{ LfgCategory.Scenario, GetDesiredScenarios },
		{ LfgCategory.RaidFinder, GetDesiredRaids }
	};

	internal static int DungeonDifficultyId => Lua.GetReturnVal<int>("return GetDungeonDifficultyID()", 0u);

	internal static int RaidDifficultyId => Lua.GetReturnVal<int>("return GetRaidDifficultyID()", 0u);

	internal static IEnumerable<LfgCategory> SupportedLfgCategories => DesiredDungeonListProviders.Keys;

	internal static void SetDungeonDifficultyId(int id)
	{
		Lua.DoString("SetDungeonDifficultyID({0})", id);
	}

	internal static void SetRaidDifficultyId(int id)
	{
		Lua.DoString("SetRaidDifficultyID({0})", id);
	}

	public static IEnumerable<LfgDungeons> GetAvailableDungeons()
	{
		using (StyxWoW.Memory.AcquireFrame())
		{
			int expansionLevel = Lua.GetReturnVal<int>("return EXPANSION_LEVEL", 0u);
			int level = StyxWoW.Me.Level;
			return LfgDungeons.Dungeons.Where(d => d.MinLevel <= level && d.MaxLevel >= level && d.ExpansionId <= expansionLevel);
		}
	}

	public static IEnumerable<LfgDungeons> GetAvailableScenarios()
	{
		using (StyxWoW.Memory.AcquireFrame())
		{
			int expansionLevel = Lua.GetReturnVal<int>("return EXPANSION_LEVEL", 0u);
			int level = StyxWoW.Me.Level;
			return LfgDungeons.Scenarios.Where(d => d.MinLevel <= level && d.MaxLevel >= level && d.ExpansionId <= expansionLevel && (d.Neutral || (StyxWoW.Me.IsAlliance && d.AllianceOnly) || (StyxWoW.Me.IsHorde && d.HordeOnly)));
		}
	}

	/// <summary>
	/// Un-queues dungeons/scenarios/raids that are no longer desired and queues the ones that are.
	/// </summary>
	public static async Task<bool> SyncQueue()
	{
		using (StyxWoW.Memory.AcquireFrame())
		{
			DungeonType dungeonType = DungeonBuddySettings.Instance.DungeonType;
			RaidType raidType = DungeonBuddySettings.Instance.RaidType;
			ScenarioType scenarioType = DungeonBuddySettings.Instance.ScenarioType;
			bool singleCategorySelected = ((dungeonType != DungeonType.None) ? 1 : 0) + ((scenarioType != ScenarioType.None) ? 1 : 0) + ((raidType != RaidType.None) ? 1 : 0) <= 1;

			foreach (KeyValuePair<LfgCategory, Func<List<LfgDungeons>>> pair in DesiredDungeonListProviders)
			{
				try
				{
					LfgCategory category = pair.Key;
					List<LfgDungeons> desired = pair.Value();
					List<LfgDungeons> queued = GetCurrentlyQueuedDungeons(category);
					bool isRaidFinder = category == LfgCategory.RaidFinder;

					foreach (LfgDungeons toRemove in queued.Except(desired))
					{
						Logger.Write("Un-queuing from {0}", toRemove.Name);
						Lua.DoString("LeaveSingleLFG({0}, {1})", (uint)category, toRemove.Id);
						await ScriptHelpers.SleepForRandomUiInteractionTime();
					}

					List<LfgDungeons> toAdd = desired.Except(queued).ToList();
					if (toAdd.Any())
					{
						await SetLfgRoles();
						Lua.DoString("ClearAllLFGDungeons({0})", (uint)category);
						foreach (LfgDungeons dungeon in toAdd)
						{
							Logger.Write("Queuing for {0}", dungeon.Name);
							Lua.DoString("SetLFGDungeon({0}, {1})", (uint)category, dungeon.Id);
							if (isRaidFinder)
							{
								Lua.DoString("JoinSingleLFG({0}, {1})", (uint)category, dungeon.Id);
								await Coroutine.Sleep(ScriptHelpers.Rnd.Next(4000, 5000));
							}
						}
						if (!isRaidFinder)
						{
							Lua.DoString("JoinLFG({0})", (uint)category);
							await ScriptHelpers.SleepForRandomUiInteractionTime();
						}
					}
				}
				catch (LfgQueueException ex)
				{
					if (singleCategorySelected)
					{
						TreeRoot.Stop(ex.Message);
						return false;
					}
					Logger.Write(Colors.Red, ex.Message);
				}
			}
			return false;
		}
	}

	internal static bool NeedsQueueSync(LfgCategory category)
	{
		List<LfgDungeons> desired = GetDesiredDungeons(category);
		List<LfgDungeons> queued = GetCurrentlyQueuedDungeons(category);
		IEnumerable<LfgDungeons> queuedButNotDesired = queued.Except(desired);
		IEnumerable<LfgDungeons> desiredButNotQueued = desired.Except(queued);
		if (!desiredButNotQueued.Any())
		{
			return queuedButNotDesired.Any();
		}
		return true;
	}

	internal static List<LfgDungeons> GetDesiredDungeons(LfgCategory category)
	{
		if (!DesiredDungeonListProviders.TryGetValue(category, out var provider))
		{
			return new List<LfgDungeons>();
		}
		return provider();
	}

	internal static Task SetLfgRoles()
	{
		Logger.WriteDebug("Setting roles.");
		DungeonBuddySettings.QueueRole role = DungeonBuddySettings.Instance.Role;
		bool tank = (role == DungeonBuddySettings.QueueRole.Auto && StyxWoW.Me.Specialization.IsTank()) || role == DungeonBuddySettings.QueueRole.Tank;
		bool healer = (role == DungeonBuddySettings.QueueRole.Auto && StyxWoW.Me.Specialization.IsHealer()) || role == DungeonBuddySettings.QueueRole.Healer;
		bool dps = (role == DungeonBuddySettings.QueueRole.Auto && StyxWoW.Me.Specialization.IsDps()) || role == DungeonBuddySettings.QueueRole.Damage;
		Lua.DoString("SetLFGRoles(GetLFGRoles(),{0},{1},{2})", tank.ToString().ToLower(), healer.ToString().ToLower(), dps.ToString().ToLower());
		return Task.CompletedTask;
	}

	private static List<LfgDungeons> GetDesiredDungeons()
	{
		List<LfgDungeons> list = new List<LfgDungeons>();
		DungeonType dungeonType = DungeonBuddySettings.Instance.DungeonType;
		if (dungeonType == DungeonType.None || dungeonType == DungeonType.Farm)
		{
			return list;
		}
		uint[] selectedDungeonIds = DungeonBuddySettings.Instance.SelectedDungeonIds;
		switch (DungeonBuddySettings.Instance.DungeonType)
		{
		case DungeonType.Specific:
			list.AddRange(from d in GetAvailableDungeons()
				where selectedDungeonIds.Contains(d.Id) && DungeonManager.HasScriptForDungeon(d.Id)
				select d);
			if (!list.Any())
			{
				throw new LfgQueueException("No vaild dungeons are selected");
			}
			break;
		case DungeonType.RandomDungeon:
		case DungeonType.RandomHeroic:
		case DungeonType.SpecialEvent:
		{
			LfgDungeons selected = null;
			if (DungeonBuddySettings.Instance.SelectedRandomDungeonId != 0)
			{
				selected = LfgDungeons.FromId(DungeonBuddySettings.Instance.SelectedRandomDungeonId);
			}
			LfgDungeons[] joinable = GetJoinableRandomDungeons();
			if (selected == null || !joinable.Contains(selected))
			{
				LfgDungeons alternative = joinable.FirstOrDefault(d => d.LfgDifficulty == LfgDungeons.Difficulty.Normal && !d.IsHolidayEvent) ?? joinable.FirstOrDefault(d => d.LfgDifficulty == LfgDungeons.Difficulty.Heroic && !d.IsHolidayEvent);
				if (alternative == null)
				{
					if (selected != null)
					{
						throw new LfgQueueException($"You can not queue for {selected.Name} and no alternative was found.");
					}
					throw new LfgQueueException("You cannot queue for any random dungeons.");
				}
				if (selected != null)
				{
					Logger.Write("You can not queue for {0} but {1} is now available.", selected.Name, alternative.Name);
				}
				else
				{
					Logger.Write("No random dungeon was chosen so automatically selecting {0}", alternative.Name);
				}
				selected = alternative;
				DungeonBuddySettings.Instance.SelectedRandomDungeonId = alternative.Id;
			}
			list.Add(selected);
			break;
		}
		}
		return list;
	}

	private static List<LfgDungeons> GetDesiredScenarios()
	{
		List<LfgDungeons> list = new List<LfgDungeons>();
		if (DungeonBuddySettings.Instance.ScenarioType == ScenarioType.None)
		{
			return list;
		}
		uint[] specifiedScenarioIds = DungeonBuddySettings.Instance.SpecifiedScenariosIds;
		switch (DungeonBuddySettings.Instance.ScenarioType)
		{
		case ScenarioType.Specific:
			list.AddRange(from d in GetAvailableScenarios()
				where specifiedScenarioIds.Contains(d.Id) && DungeonManager.HasScriptForDungeon(d.Id)
				select d);
			if (!list.Any())
			{
				throw new LfgQueueException("No vaild scenarios are selected");
			}
			break;
		case ScenarioType.Random:
			if (DungeonBuddySettings.Instance.SelectedScenarioId != 0)
			{
				list.Add(LfgDungeons.FromId(DungeonBuddySettings.Instance.SelectedScenarioId));
			}
			break;
		}
		return list;
	}

	private static List<LfgDungeons> GetDesiredRaids()
	{
		List<LfgDungeons> list = new List<LfgDungeons>();
		RaidType raidType = DungeonBuddySettings.Instance.RaidType;
		if (raidType == RaidType.None || raidType == RaidType.Farm)
		{
			return list;
		}
		list.AddRange(from d in GetJoinableRandomRaids()
			where DungeonBuddySettings.Instance.SelectedRaidIds.Contains(d.Id) && DungeonManager.HasScriptForDungeon(d.Id) && (raidType == RaidType.Specific || !HasCompletedLfr(d.Id))
			orderby d.OrderIndex descending
			select d);
		if (!list.Any())
		{
			if (raidType == RaidType.Specific)
			{
				throw new LfgQueueException("No LFRs are selected.");
			}
			throw new LfgQueueException("You have completed all of the selected LFRs for this week");
		}
		return list.OrderByDescending(d => d.OrderIndex).Take(5).ToList();
	}

	internal static List<LfgDungeons> GetCurrentlyQueuedDungeons(LfgCategory category)
	{
		List<string> returnValues = Lua.GetReturnValues($"local queuedList = GetLFGQueuedList({(int)category}) local ret={{}} for k,_ in pairs(queuedList) do ret[#ret+1]=k end return unpack(ret)");
		return (from d in returnValues.ConvertAll<uint>(uint.Parse).Select(LfgDungeons.FromId)
			where d?.IsValid ?? false
			select d).ToList();
	}

	/// <summary>
	/// Returns true if the raid finder wing has already had the required number of bosses killed this week.
	/// </summary>
	public static bool HasCompletedLfr(uint lfgId)
	{
		string lua = $"return select(2,GetLFGDungeonNumEncounters({lfgId}))";
		uint bossesKilled = Lua.GetReturnVal<uint>(lua, 0u);
		if (!LfrBossCounts.TryGetValue(lfgId, out var requiredBossCount))
		{
			Logger.Write(Colors.Red, "Warning: lfgId {0} has not been added to LfrBossCount", lfgId);
			return false;
		}
		return requiredBossCount == bossesKilled;
	}

	public static LfgDungeons[] GetJoinableRandomDungeons()
	{
		return GetJoinableRandom(RandomQueueKind.Dungeon);
	}

	public static LfgDungeons[] GetJoinableRandomRaids()
	{
		return GetJoinableRandom(RandomQueueKind.Raid);
	}

	public static LfgDungeons[] GetJoinableRandomScenarios()
	{
		return GetJoinableRandom(RandomQueueKind.Scenario);
	}

	private static LfgDungeons[] GetJoinableRandom(RandomQueueKind kind)
	{
		List<LfgDungeons> list = new List<LfgDungeons>();
		string countCall = string.Empty;
		string infoCall = string.Empty;
		switch (kind)
		{
		case RandomQueueKind.Dungeon:
			countCall = "GetNumRandomDungeons()";
			infoCall = "GetLFGRandomDungeonInfo(i)";
			break;
		case RandomQueueKind.Raid:
			countCall = "GetNumRFDungeons()";
			infoCall = "GetRFDungeonInfo(i)";
			break;
		case RandomQueueKind.Scenario:
			countCall = "GetNumRandomScenarios()";
			infoCall = "GetRandomScenarioInfo(i)";
			break;
		}
		List<string> returnValues = Lua.GetReturnValues($"local myLvl = UnitLevel('player')\r\n        local ids = {{}}\r\n        for i =1, {countCall} do\r\n           local id,_,_,_,minLvl,maxLvl,_,_,_,xpac = {infoCall}\r\n           local canDisplay = myLvl >= minLvl and myLvl <= maxLvl and EXPANSION_LEVEL >= xpac\r\n           if canDisplay and IsLFGDungeonJoinable(id) then \r\n              table.insert(ids,id)      \r\n           end\r\n        end\r\n        return unpack(ids)\r\n        ");
		if (returnValues != null && returnValues.Count > 0)
		{
			list.AddRange(returnValues.Select(s => LfgDungeons.FromId(uint.Parse(s))));
		}
		return list.ToArray();
	}
}
