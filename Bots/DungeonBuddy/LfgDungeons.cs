using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Styx;
using Styx.Patchables;
using Styx.WoWInternals;
using Styx.WoWInternals.DBC;

namespace Bots.DungeonBuddy;

public class LfgDungeons : IEquatable<LfgDungeons>
{
	[StructLayout(LayoutKind.Sequential)]
	private struct LfgDungeonStruct
	{
		public uint Id;
		public IntPtr NamePtr;
		public int MinLvl;
		public int MaxLvl;
		public int RecommendedLvl;
		public int MinQueueLevel;
		public int MaxQueueLevel;
		public uint MapId;
		public int DifficultyId;
		public LfgDungeonsFlags Flags;
		public int TypeId;
		public int FactionId;
		public IntPtr TexturePtr;
		public int ExpansionLevel;
		public int OrderIndex;
		public uint GroupId;
		public IntPtr DescriptionPtr;
		public uint RandomId;
		public int MaxTanks;
		public int MaxHealers;
		public int MaxDps;
		public int MinTanks;
		public int MinHealers;
		public int MinDps;
		public int ScenarioId;
		public LfgType Type;
		public int BonusRepAmount;
		// two trailing DB fields with no known use in DungeonBuddy; kept for correct row layout.
		private readonly int _reserved1;
		private readonly int _reserved2;
	}

	public enum LfgType
	{
		Zone,
		Dungeon,
		Raid,
		Scenario,
		FlexibleRaid
	}

	public enum Difficulty
	{
		None,
		Normal,
		Heroic,
		TenMan,
		TwentyFiveMan,
		TenManHeroic,
		TwentyFiveManHeroic,
		RaidFinder,
		Challenger,
		FourtyMan
	}

	[Flags]
	public enum LfgDungeonsFlags
	{
		HasQueue = 1,
		IsHoliday = 4,
		IsWeeklyDungeon = 0x10,
		IsQueueableRaid = 0x40,
		IsWeeklyScenario = 0x200,
		ForceHide = 0x400,
		UserTeleportBlocked = 0x800,
		RequiresGroup = 0x1000
	}

	private LfgDungeonStruct _lfgDungeonStruct;
	private readonly WoWDb.Row _row;
	private string _description;
	private Map _map;
	private string _name;
	private LfgDungeons _random;
	private string _textureFile;

	public static IEnumerable<LfgDungeons> Collection => StyxWoW.Db[ClientDb.LfgDungeons].Select(row => new LfgDungeons(row));

	public static IEnumerable<LfgDungeons> Dungeons => Collection.Where(d => d.SubType == LfgType.Dungeon && d.TypeId != 6 && !d.IsHolidayEvent);

	public static IEnumerable<LfgDungeons> Raids => Collection.Where(d => d.SubType == LfgType.Raid && d.LfgDifficulty != Difficulty.RaidFinder);

	public static IEnumerable<LfgDungeons> LfRaids => Collection.Where(d => d.SubType == LfgType.Raid && d.LfgDifficulty == Difficulty.RaidFinder);

	public static IEnumerable<LfgDungeons> Scenarios => Collection.Where(d => d.SubType == LfgType.Scenario && d.TypeId != 6);

	public static IEnumerable<LfgDungeons> Zones => Collection.Where(d => d.SubType == LfgType.Zone);

	public static IEnumerable<LfgDungeons> WorldEvents => Collection.Where(d => d.SubType == LfgType.Dungeon && d.IsHolidayEvent);

	public static IEnumerable<LfgDungeons> FlexRaids => Collection.Where(d => d.SubType == LfgType.FlexibleRaid);

	public bool AllianceOnly => FactionId == 1;

	public string Description => _description ?? (_description = _row.GetStringField(16u));

	public int BonusRepAmount => _lfgDungeonStruct.BonusRepAmount;

	/// <summary>
	/// Returns the dungeon difficulty, 1 = Normal 5man, 2 = Heroic 5man, 3 = 10man raid, 4 = 25man raid, 5 = 10man heroic raid, 6 - 25man heroic raid, 7 = LFRaid, 8 = Challenger Mode, 9 = 40man Mode
	/// </summary>
	public Difficulty LfgDifficulty => (Difficulty)_lfgDungeonStruct.DifficultyId;

	public int DifficultyId => _lfgDungeonStruct.DifficultyId;

	public int MinDpsCount => _lfgDungeonStruct.MinDps;

	public int MaxDpsCount => _lfgDungeonStruct.MaxDps;

	public int ExpansionId => _lfgDungeonStruct.ExpansionLevel;

	// This is NOT an index into the Faction DBC!
	// -1 = All faction, 0 = Horde, 1 = Alliance
	public int FactionId => _lfgDungeonStruct.FactionId;

	public bool ForceHide
	{
		get
		{
			if (Flags.HasFlag(LfgDungeonsFlags.ForceHide))
			{
				return (StyxWoW.Me.Flags2 & 0x200) != 0;
			}
			return false;
		}
	}

	public LfgDungeonsFlags Flags => _lfgDungeonStruct.Flags;

	public uint GroupId => _lfgDungeonStruct.GroupId;

	public int MaxHealerCount => _lfgDungeonStruct.MaxHealers;

	public int MinHealerCount => _lfgDungeonStruct.MinHealers;

	public bool HordeOnly => FactionId == 0;

	public uint Id => _lfgDungeonStruct.Id;

	public bool IsValid => _row != null && _row.IsValid;

	public bool IsHolidayEvent => Flags.HasFlag(LfgDungeonsFlags.IsHoliday);

	public bool IsUserTeleportAllowed => !Flags.HasFlag(LfgDungeonsFlags.UserTeleportBlocked);

	public Map Map => _map ?? (_map = Map.FromId(MapId));

	public uint MapId => _lfgDungeonStruct.MapId;

	public int MaxLevel => _lfgDungeonStruct.MaxLvl;

	public int MinLevel => _lfgDungeonStruct.MinLvl;

	public string Name => _name ?? (_name = _row.GetStringField(1u));

	public bool Neutral => FactionId == -1;

	public int OrderIndex => _lfgDungeonStruct.OrderIndex;

	public LfgDungeons Random
	{
		get
		{
			if (RandomId == 0)
			{
				return null;
			}
			return _random ?? (_random = new LfgDungeons(RandomId));
		}
	}

	public uint RandomId => _lfgDungeonStruct.RandomId;

	public int QueueMaxLevel => _lfgDungeonStruct.MaxQueueLevel;

	public int QueueMinLevel => _lfgDungeonStruct.MinQueueLevel;

	public int RecommendedLevel => _lfgDungeonStruct.RecommendedLvl;

	public bool RequiresGroup => Flags.HasFlag(LfgDungeonsFlags.RequiresGroup);

	public int ScenarioId => _lfgDungeonStruct.ScenarioId;

	public int MinSize => MinTankCount + MinHealerCount + MinDpsCount;

	public int MaxSize => MaxTankCount + MaxHealerCount + MaxDpsCount;

	public int MinTankCount => _lfgDungeonStruct.MinTanks;

	public int MaxTankCount => _lfgDungeonStruct.MaxTanks;

	public string TextureFileName => _textureFile ?? (_textureFile = _row.GetStringField(12u));

	public LfgType SubType => _lfgDungeonStruct.Type;

	public int TypeId => _lfgDungeonStruct.TypeId;

	public static LfgDungeons FromId(uint id)
	{
		return new LfgDungeons(id);
	}

	private LfgDungeons(uint id)
		: this(StyxWoW.Db[ClientDb.LfgDungeons].GetRow(id))
	{
	}

	private LfgDungeons(WoWDb.Row row)
	{
		_row = row;
		if (_row != null && _row.IsValid)
		{
			_lfgDungeonStruct = _row.GetStruct<LfgDungeonStruct>();
		}
	}

	public override bool Equals(object obj)
	{
		if (obj == null || GetType() != obj.GetType())
		{
			return false;
		}
		return Equals((LfgDungeons)obj);
	}

	public override int GetHashCode()
	{
		return Id.GetHashCode();
	}

	public bool Equals(LfgDungeons other)
	{
		return other.Id == Id;
	}
}
