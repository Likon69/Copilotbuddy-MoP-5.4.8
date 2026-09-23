using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Media;
using Bots.DungeonBuddy.Helpers;
using Bots.DungeonBuddy.Profiles.Handlers;
using Styx;
using Styx.Common;
using Styx.Helpers;
using Styx.WoWInternals;
using Styx.WoWInternals.DBC;

namespace Bots.DungeonBuddy;

public static class DungeonManager
{
	private struct SharedMapDungeonEntry
	{
		public int LfgId;

		public int MapId;

		public int[] MapLevels;
	}

	private static int _lastCorpseMapId = -1;

	private static AssemblyLoader<Dungeon> _defaultDungeonLoader;

	private static AssemblyLoader<Dungeon> _customDungeonLoader;

	private static Dungeon _selectedDungeon;

	private static Dungeon _corpseDungeon;

	private static TimeCachedValue<Dungeon> _currentDungeonCache;

	private static EventHandler<EventArgs> _onInitialized;

	private static readonly List<SharedMapDungeonEntry> SharedMapDungeonEntries = new List<SharedMapDungeonEntry>
	{
		new SharedMapDungeonEntry { LfgId = 38, MapId = 429, MapLevels = new int[1] { 1 } },
		new SharedMapDungeonEntry { LfgId = 36, MapId = 429, MapLevels = new int[3] { 2, 3, 4 } },
		new SharedMapDungeonEntry { LfgId = 34, MapId = 429, MapLevels = new int[2] { 5, 6 } },
		new SharedMapDungeonEntry { LfgId = 30, MapId = 230, MapLevels = new int[2] { 1, 2 } },
		new SharedMapDungeonEntry { LfgId = 276, MapId = 230, MapLevels = new int[2] { 1, 2 } },
		new SharedMapDungeonEntry { LfgId = 40, MapId = 329, MapLevels = new int[1] { 1 } },
		new SharedMapDungeonEntry { LfgId = 274, MapId = 329, MapLevels = new int[1] { 2 } },
		new SharedMapDungeonEntry { LfgId = 272, MapId = 349, MapLevels = new int[2] { 1, 2 } },
		new SharedMapDungeonEntry { LfgId = 26, MapId = 349, MapLevels = new int[2] { 1, 2 } },
		new SharedMapDungeonEntry { LfgId = 273, MapId = 349, MapLevels = new int[2] { 1, 2 } },
		new SharedMapDungeonEntry { LfgId = 32, MapId = 229, MapLevels = new int[4] { 1, 2, 3, 4 } },
		new SharedMapDungeonEntry { LfgId = 330, MapId = 229, MapLevels = new int[5] { 3, 4, 5, 6, 7 } }
	};

	private static PerFrameCachedValue<LfgDungeons.Difficulty> _instanceDifficultyCache;

	private static PerFrameCachedValue<int> _currentDungeonMapLevelCache;

	public static bool Initialized { get; private set; }

	private static string ScriptsPath => Path.Combine(DungeonBot.HbPath, "Dungeon Scripts");

	public static ReadOnlyCollection<Dungeon> DefaultDungeons { get; private set; }

	public static ReadOnlyCollection<Dungeon> CustomDungeons { get; private set; }

	public static Dungeon SelectedDungeon
	{
		get
		{
			return _selectedDungeon;
		}
		set
		{
			_selectedDungeon = value;
			_lastCorpseMapId = -1;
		}
	}

	public static Dungeon CurrentDungeon
	{
		get
		{
			if (!Initialized)
			{
				return null;
			}
			return _currentDungeonCache ?? (_currentDungeonCache = new TimeCachedValue<Dungeon>(TimeSpan.FromSeconds(1.0), GetCurrentDungeon));
		}
	}

	public static Dungeon CorpseDungeon
	{
		get
		{
			if (!Initialized)
			{
				return null;
			}
			int corpseMapId = StyxWoW.Me.CorpseMapId;
			if (_lastCorpseMapId != corpseMapId)
			{
				_corpseDungeon = GetDungeonForMapId(corpseMapId);
				_lastCorpseMapId = corpseMapId;
			}
			return _corpseDungeon;
		}
	}

	private static LfgDungeons.Difficulty InstanceDifficulty => _instanceDifficultyCache ?? (_instanceDifficultyCache = new PerFrameCachedValue<LfgDungeons.Difficulty>(() => (LfgDungeons.Difficulty)Lua.GetReturnVal<int>("local _,_,d=GetInstanceInfo() if d ~= nil then return d end return 1", 0u)));

	private static int CurrentDungeonMapLevel => _currentDungeonMapLevelCache ?? (_currentDungeonMapLevelCache = new PerFrameCachedValue<int>(() => Lua.GetReturnVal<int>("return GetCurrentMapDungeonLevel()", 0u)));

	public static event EventHandler<EventArgs> OnInitialized
	{
		add { _onInitialized += value; }
		remove { _onInitialized -= value; }
	}

	private static Dungeon GetDungeonForMapId(int mapId)
	{
		if (mapId == -1)
		{
			return null;
		}
		List<Dungeon> candidates = CustomDungeons.Where(d => d.LfgDungeon.MapId == (uint)mapId).ToList();
		if (!candidates.Any())
		{
			candidates = DefaultDungeons.Where(d => d.LfgDungeon.MapId == mapId).ToList();
		}
		return candidates.OrderBy(d => d.Entrance.DistanceSqr(StyxWoW.Me.Location)).FirstOrDefault();
	}

	private static Dungeon GetCurrentDungeon()
	{
		Map map = StyxWoW.Me.CurrentMap;
		if (map.MapType != MapType.Raid && map.MapType != MapType.Instance && map.MapType != MapType.Scenario)
		{
			return null;
		}
		List<Dungeon> candidates = CustomDungeons.Where(d => d.LfgDungeon.MapId == map.MapId && d.LfgDungeon.LfgDifficulty == InstanceDifficulty).ToList();
		if (!candidates.Any())
		{
			candidates = DefaultDungeons.Where(d => d.LfgDungeon.MapId == map.MapId && d.LfgDungeon.LfgDifficulty == InstanceDifficulty).ToList();
		}
		if (candidates.Count <= 1)
		{
			return candidates.FirstOrDefault();
		}
		// several scripts share the same map id (e.g. Dire Maul wings); disambiguate using the dungeon's sub-level.
		List<Dungeon> matchingSharedMapLevel = (from candidate in candidates
			select new { match = candidate, sharedMapEntry = SharedMapDungeonEntries.FirstOrDefault(e => candidate.DungeonId == e.LfgId) }
			into pair
			where pair.sharedMapEntry.MapLevels != null && pair.sharedMapEntry.MapLevels.Contains(CurrentDungeonMapLevel)
			select pair.match).ToList();
		if (matchingSharedMapLevel.Count == 1)
		{
			return matchingSharedMapLevel.FirstOrDefault();
		}
		if (DungeonBot.CurrentLfgDungeonId != 0)
		{
			Dungeon byLfgId = candidates.FirstOrDefault(d => d.DungeonId == DungeonBot.CurrentLfgDungeonId);
			if (byLfgId != null)
			{
				return byLfgId;
			}
		}
		if (SelectedDungeon != null && candidates.Any(d => d.LfgDungeon.MapId == SelectedDungeon.LfgDungeon.MapId))
		{
			return SelectedDungeon;
		}
		return candidates.FirstOrDefault(d => !d.LfgDungeon.IsHolidayEvent);
	}

	public static void Initialize()
	{
		_defaultDungeonLoader = new AssemblyLoader<Dungeon>(ScriptsPath, detectFileChanges: true);
		_defaultDungeonLoader.Reload("Compiling Dungeonbuddy scripts");
		DefaultDungeons = (_defaultDungeonLoader != null) ? _defaultDungeonLoader.Instances.AsReadOnly() : new ReadOnlyCollection<Dungeon>(new List<Dungeon>());
		_defaultDungeonLoader.Reloaded += OnDungeonScriptsReloaded;
		if (!string.IsNullOrWhiteSpace(DungeonBuddySettings.Instance.CustomProfileAndScriptsPath) && Directory.Exists(DungeonBuddySettings.Instance.CustomProfileAndScriptsPath))
		{
			_customDungeonLoader = new AssemblyLoader<Dungeon>(DungeonBuddySettings.Instance.CustomProfileAndScriptsPath, detectFileChanges: true);
			_customDungeonLoader.Reload("Compiling custom Dungeonbuddy scripts");
			CustomDungeons = (_customDungeonLoader != null) ? _customDungeonLoader.Instances.AsReadOnly() : new ReadOnlyCollection<Dungeon>(new List<Dungeon>());
			_customDungeonLoader.Reloaded += OnDungeonScriptsReloaded;
		}
		else
		{
			_customDungeonLoader = null;
			CustomDungeons = new ReadOnlyCollection<Dungeon>(new List<Dungeon>());
		}
		Logger.WriteDebug("Dungeon Scripts are loaded.");
		Initialized = true;
		_onInitialized?.Invoke(null, EventArgs.Empty);
	}

	public static bool HasScriptForDungeon(uint lfgId)
	{
		if (!CustomDungeons.Any(d => d.DungeonId == lfgId))
		{
			return DefaultDungeons.Any(d => d.DungeonId == lfgId);
		}
		return true;
	}

	private static void OnDungeonScriptsReloaded(object sender, EventArgs e)
	{
		DefaultDungeons = (_defaultDungeonLoader != null) ? _defaultDungeonLoader.Instances.AsReadOnly() : new ReadOnlyCollection<Dungeon>(new List<Dungeon>());
		CustomDungeons = (_customDungeonLoader != null) ? _customDungeonLoader.Instances.AsReadOnly() : new ReadOnlyCollection<Dungeon>(new List<Dungeon>());
		if (SelectedDungeon == null)
		{
			return;
		}
		Logger.Write(Colors.Orchid, "Reloaded dungeon scripts", (sender == _customDungeonLoader) ? "custom" : "default");
		List<Boss> originalBossEncounters = BossManager.BossEncounters;
		uint dungeonId = SelectedDungeon.DungeonId;
		DungeonBot.Instance.HandleDungeonLeft();
		DungeonBot.Instance.HandleDungeonEntered(dungeonId);
		List<Boss> newBossEncounters = BossManager.BossEncounters;
		if (originalBossEncounters.Count != newBossEncounters.Count)
		{
			return;
		}
		for (int i = 0; i < newBossEncounters.Count; i++)
		{
			if (!originalBossEncounters[i].IsAlive)
			{
				newBossEncounters[i].MarkAsDead("Script reload.");
			}
		}
	}
}
