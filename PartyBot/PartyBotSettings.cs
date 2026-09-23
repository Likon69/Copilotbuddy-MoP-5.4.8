using System.ComponentModel;
using System.IO;
using Styx.Helpers;

namespace PartyBot;

public class PartyBotSettings : Settings
{
	public const int MaxFollowDistance = 40;

	public const int MinFollowDistance = 5;

	public static readonly PartyBotSettings Instance = new PartyBotSettings();

	private int _followDistance;

	private bool _autoAcceptSharedQuests;

	private bool _acceptBattlefieldPorts;

	private bool _acceptDungeonInvites;

	private bool _lootInDungeons;

	private bool _waitForRessInDungeons;

	private string _leaderName;

	[DisplayName("Auto Accept Shared Quests")]
	[Setting]
	[Description("True if the bot should auto accept shared quests")]
	[Category("Misc")]
	[Styx.Helpers.DefaultValue(false)]
	public bool AutoAcceptSharedQuests
	{
		get
		{
			return _autoAcceptSharedQuests;
		}
		set
		{
			_autoAcceptSharedQuests = value;
		}
	}

	[Category("Misc")]
	[Description("True if the bot should accept battlefield ports")]
	[Setting]
	[Styx.Helpers.DefaultValue(false)]
	[DisplayName("Accept Battlefield Ports")]
	public bool AcceptBattlefieldPorts
	{
		get
		{
			return _acceptBattlefieldPorts;
		}
		set
		{
			_acceptBattlefieldPorts = value;
		}
	}

	[Setting]
	[Category("Dungeon")]
	[DisplayName("Accept Dungeon Invites")]
	[Description("True if the bot should accept dungeon invites")]
	[Styx.Helpers.DefaultValue(false)]
	public bool AcceptDungeonInvites
	{
		get
		{
			return _acceptDungeonInvites;
		}
		set
		{
			_acceptDungeonInvites = value;
		}
	}

	[Description("True if the bot should loot in dungeons")]
	[DisplayName("Loot In Dungeons")]
	[Setting]
	[Styx.Helpers.DefaultValue(false)]
	[Category("Dungeon")]
	public bool LootInDungeons
	{
		get
		{
			return _lootInDungeons;
		}
		set
		{
			_lootInDungeons = value;
		}
	}

	[Category("Dungeon")]
	[Description("True if the bot should wait before releasing from corpse in dungeons")]
	[Setting]
	[Styx.Helpers.DefaultValue(true)]
	[DisplayName("Wait For Ress In Dungeons")]
	public bool WaitForRessInDungeons
	{
		get
		{
			return _waitForRessInDungeons;
		}
		set
		{
			_waitForRessInDungeons = value;
		}
	}

	[Setting]
	[DisplayName("Leader Name")]
	[Description("The name of the leader. Add a hyphen and server name if leader is on another server. e.g. LEADERNAME-SERVERNAME. Should remain empty for leader.")]
	[Styx.Helpers.DefaultValue("")]
	[Category("Party")]
	public string LeaderName
	{
		get
		{
			return _leaderName;
		}
		set
		{
			_leaderName = value;
		}
	}

	[Description("The followers will attempt to stay within this distance to leader. Distance must be between 5 and 40")]
	[Styx.Helpers.DefaultValue(15)]
	[DisplayName("Follow Distance")]
	[Setting]
	[Category("Party")]
	public int FollowDistance
	{
		get
		{
			return _followDistance;
		}
		set
		{
			if (value > 40)
			{
				_followDistance = 40;
			}
			else if (value < 5)
			{
				_followDistance = 5;
			}
			else
			{
				_followDistance = value;
			}
		}
	}

	public PartyBotSettings()
		: base(Path.Combine(Settings.CharacterSettingsDirectory, "PartyBotSettings.xml"))
	{
	}
}
