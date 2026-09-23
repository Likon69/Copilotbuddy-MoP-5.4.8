using System;
using System.Collections.Generic;
using System.Linq;
using Bots.DungeonBuddy.Helpers;
using JetBrains.Annotations;
using Styx;
using Styx.Helpers;
using Styx.Patchables;
using Styx.WoWInternals;
using Styx.WoWInternals.DBC;
using Styx.WoWInternals.WoWObjects;

namespace Bots.DungeonBuddy;

public class GroupMember : IEquatable<GroupMember>
{
	private WoWPartyMember _member;

	private string _name;

	private WoWPlayer _player;

	private string _server;

	private static readonly List<GroupMember> _groupMembers = new List<GroupMember>();

	private static PerFrameCachedValue<List<GroupMember>> _cachedGroupMembers;

	private static string _unknownObjectName;

	public WoWPlayer Player
	{
		get
		{
			if (_player == null || !_player.IsValid)
			{
				_player = _member.ToPlayer();
			}
			return _player;
		}
	}

	public WoWPoint Location
	{
		get
		{
			WoWPlayer player = Player;
			if (player == null)
			{
				return _member.Location3D;
			}
			return player.Location;
		}
	}

	public bool IsDead
	{
		get
		{
			WoWPlayer player = Player;
			if (player == null)
			{
				return _member.Dead;
			}
			if (!player.IsDead)
			{
				return player.Shapeshift == ShapeshiftForm.SpiritOfRedemption;
			}
			return true;
		}
	}

	public bool IsGhost
	{
		get
		{
			WoWPlayer player = Player;
			if (player == null)
			{
				return _member.Ghost;
			}
			return player.IsGhost;
		}
	}

	public float Distance => Location.Distance(StyxWoW.Me.Location);

	public bool IsAlive
	{
		get
		{
			if (!IsDead)
			{
				return !IsGhost;
			}
			return false;
		}
	}

	[NotNull]
	public string Name
	{
		get
		{
			if (string.IsNullOrEmpty(_name) || _name == UnknownObjectName)
			{
				TryGetNameAndServer(out _name, out _server);
			}
			return _name;
		}
	}

	[NotNull]
	public string Server
	{
		get
		{
			if (string.IsNullOrEmpty(_server) || _server == UnknownObjectName)
			{
				TryGetNameAndServer(out _name, out _server);
			}
			return _server;
		}
	}

	public uint MapId
	{
		get
		{
			uint myMapId = StyxWoW.Me.MapId;
			if (Player != null)
			{
				return myMapId;
			}
			WoWDb.Row row = StyxWoW.Db[ClientDb.AreaTable].GetRow((uint)_member.AreaTableId);
			if (row != null && row.IsValid)
			{
				return row.GetField<uint>(1u);
			}
			return myMapId;
		}
	}

	public Map CurrentMap => Map.FromId(MapId);

	public float CurrentHealth
	{
		get
		{
			WoWPlayer player = Player;
			return (player != null) ? player.CurrentHealth : _member.Health;
		}
	}

	public float MaxHealth
	{
		get
		{
			WoWPlayer player = Player;
			return (player != null) ? player.MaxHealth : _member.HealthMax;
		}
	}

	public double HealthPercent
	{
		get
		{
			WoWPlayer player = Player;
			if (player == null)
			{
				return (double)_member.Health * 100.0 / (double)_member.HealthMax;
			}
			return player.HealthPercent;
		}
	}

	public WoWPartyMember InternalInfo => _member;

	public WoWSpec Specialization
	{
		get
		{
			if (Player == null)
			{
				return _member.Specialization;
			}
			return Player.Specialization;
		}
	}

	public bool IsOnline
	{
		get
		{
			if (Player == null)
			{
				return _member.IsOnline;
			}
			return true;
		}
	}

	public int Level
	{
		get
		{
			WoWPlayer player = Player;
			if (player == null)
			{
				return _member.Level;
			}
			return player.Level;
		}
	}

	public WoWPartyMember.GroupRole Role => _member.Role;

	public uint CurrentPower
	{
		get
		{
			WoWPlayer player = Player;
			if (player == null)
			{
				return _member.Power;
			}
			return player.CurrentPower;
		}
	}

	public uint MaxPower
	{
		get
		{
			WoWPlayer player = Player;
			if (player == null)
			{
				return _member.PowerMax;
			}
			return player.MaxPower;
		}
	}

	public float PowerPercent
	{
		get
		{
			WoWPlayer player = Player;
			if (player == null)
			{
				return (float)_member.PowerMax * 100f / (float)_member.PowerMax;
			}
			return player.PowerPercent;
		}
	}

	public ulong Guid => _member.Guid;

	/// <summary>Determines whether the specified party member is range.</summary>
	public bool IsRange => Specialization.IsRange();

	/// <summary>Determines whether the specified party member is melee.</summary>
	public bool IsMelee => Specialization.IsMelee();

	/// <summary>Determines whether the specified party member is tank.</summary>
	public bool IsTank
	{
		get
		{
			if (!_member.HasRole(WoWPartyMember.GroupRole.Tank))
			{
				if (Specialization.IsTank())
				{
					return _member.Role == WoWPartyMember.GroupRole.None;
				}
				return false;
			}
			return true;
		}
	}

	/// <summary>Determines whether the specified party member is healer.</summary>
	public bool IsHealer
	{
		get
		{
			if (!_member.HasRole(WoWPartyMember.GroupRole.Healer))
			{
				return Specialization.IsHealer();
			}
			return true;
		}
	}

	/// <summary>Determines whether the specified party member is dps.</summary>
	public bool IsDps
	{
		get
		{
			if (!_member.HasRole(WoWPartyMember.GroupRole.Damage))
			{
				return Specialization.IsDps();
			}
			return true;
		}
	}

	public string UnitId
	{
		get
		{
			if (_member.Guid == StyxWoW.Me.Guid)
			{
				return "player";
			}
			bool isInRaid = StyxWoW.Me.GroupInfo.IsInRaid;
			ulong[] guids = isInRaid ? StyxWoW.Me.GroupInfo.RaidMemberGuids : StyxWoW.Me.GroupInfo.PartyMemberGuids;
			int index = Array.IndexOf(guids, _member.Guid);
			if (index >= 0)
			{
				return (StyxWoW.Me.GroupInfo.IsInRaid ? "raid" : "party") + (index + 1);
			}
			return "none";
		}
	}

	public static List<GroupMember> GroupMembers => _cachedGroupMembers ?? (_cachedGroupMembers = new PerFrameCachedValue<List<GroupMember>>(() => Pulse(_groupMembers)));

	private static string UnknownObjectName => _unknownObjectName ?? (_unknownObjectName = Lua.GetReturnVal<string>("return UNKNOWNOBJECT", 0u));

	internal GroupMember(WoWPartyMember member)
	{
		_member = member;
	}

	public bool NameEquals(string fullName)
	{
		if (string.IsNullOrEmpty(fullName))
		{
			return false;
		}
		fullName = fullName.Trim();
		string server = null;
		string name;
		if (fullName.Any(c => c == '-'))
		{
			string[] parts = fullName.Split('-');
			if (parts.Length != 2)
			{
				return false;
			}
			name = parts[0].Trim();
			server = parts[1].Replace(" ", "").Trim();
		}
		else
		{
			name = fullName;
		}
		if (Name.Equals(name, StringComparison.OrdinalIgnoreCase))
		{
			if (server != null)
			{
				return Server.Equals(server, StringComparison.OrdinalIgnoreCase);
			}
			return true;
		}
		return false;
	}

	private static List<GroupMember> Pulse(List<GroupMember> groupMembers)
	{
		List<WoWPartyMember> raidMembers = StyxWoW.Me.GroupInfo.RaidMembers.ToList();
		int removedCount = 0;
		for (int i = groupMembers.Count - 1; i >= 0; i--)
		{
			GroupMember groupMember = groupMembers[i];
			WoWPartyMember member = raidMembers.FirstOrDefault(m => m.Guid == groupMember._member.Guid);
			if (member == null)
			{
				groupMembers.RemoveAt(i);
				removedCount++;
			}
			else
			{
				groupMember._member = member;
			}
		}
		List<GroupMember> newMembers = (from member in raidMembers
			where groupMembers.All(groupMember => groupMember.InternalInfo != member)
			select new GroupMember(member)).ToList();
		if (newMembers.Any())
		{
			groupMembers.AddRange(newMembers);
		}
		if (removedCount > 0 || newMembers.Any())
		{
			switch (groupMembers.Count)
			{
			default:
			{
				int delta = newMembers.Count - removedCount;
				Logger.WriteDebug("{0} member{1} {2} the group. Total number of group members: {3}", Math.Abs(delta), (Math.Abs(delta) == 1) ? "" : "s", (delta > 0) ? "joined" : "left", groupMembers.Count);
				break;
			}
			case 0:
				Logger.WriteDebug("I am no-longer in a group");
				break;
			case 1:
				Logger.WriteDebug("I'm the only one left in group");
				break;
			}
		}
		return groupMembers;
	}

	public bool Equals(GroupMember other)
	{
		if ((object)other == null)
		{
			return false;
		}
		return _member.Guid == other.Guid;
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (!(obj is GroupMember groupMember))
		{
			return false;
		}
		return groupMember.Guid == _member.Guid;
	}

	public override int GetHashCode()
	{
		return _member.Guid.GetHashCode();
	}

	public static bool operator ==(GroupMember a, GroupMember b)
	{
		if (ReferenceEquals(a, b))
		{
			return true;
		}
		if ((object)a != null && (object)b != null)
		{
			return a.Guid == b.Guid;
		}
		return false;
	}

	public static bool operator !=(GroupMember a, GroupMember b)
	{
		return !(a == b);
	}

	private bool TryGetNameAndServer(out string name, out string server)
	{
		string lua = $"local n,r=UnitName(\"{UnitId}\") if not n then return nil end if not r or r=='' then r=GetRealmName() end return n,r";
		List<string> returnValues = Lua.GetReturnValues(lua);
		if (returnValues != null && returnValues.Count >= 2)
		{
			name = returnValues[0];
			server = returnValues[1];
			return true;
		}
		name = (server = "");
		return false;
	}
}
