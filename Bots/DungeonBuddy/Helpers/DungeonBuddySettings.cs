using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing.Design;
using System.IO;
using System.Windows.Forms.Design;
using Bots.DungeonBuddy.Enums;
using Styx;
using Styx.Helpers;
using Styx.WoWInternals;

namespace Bots.DungeonBuddy.Helpers;

public class DungeonBuddySettings : Settings
{
	public enum QueueRole
	{
		Auto,
		Tank,
		Healer,
		Damage
	}

	public static readonly DungeonBuddySettings Instance;

	[Description("True if party mode is enabled, false otherwise. Should be set to true for each bot")]
	[DisplayName("Party Mode")]
	[Setting]
	[Styx.Helpers.DefaultValue(PartyMode.Off)]
	[Category("Party")]
	public PartyMode PartyMode { get; set; }

	[Setting]
	[DisplayName("Party Member Names")]
	[Description("This should only contain the names of the followers. Only set this for the leader bot. Should remain empty for followers")]
	[Category("Party")]
	[TypeConverter(typeof(StringArrayConverter))]
	public string[] PartyMembers { get; set; }

	[Setting]
	[DisplayName("Leader Name")]
	[Description("This should contain the name of the Leader. Only set this on the followers. Should remain empty for leader.")]
	[Category("Party")]
	public string LeaderName { get; set; }

	[Setting]
	[Styx.Helpers.DefaultValue(true)]
	[DisplayName("Port Outside On Ghost Sighting")]
	[Category("Misc")]
	[Description("Toggles whether to port outside and back in when someone in group dies and releases to meet up at entrance")]
	public bool PortOutsideOnGhostSighting { get; set; }

	[Category("Loot")]
	[DisplayName("Loot Mode")]
	[Description("Set to always to loot every mob (warning may lose sight of tank)")]
	[Setting]
	[Styx.Helpers.DefaultValue(LootMode.BossesOnly)]
	public LootMode LootMode { get; set; }

	[DisplayName("Free-For-All Loot")]
	[Description("Set Group loot to 'Free for all' when this is enabled and bot is group leader. Only set when group leader changes or bot is started.")]
	[Setting]
	[Styx.Helpers.DefaultValue(false)]
	[Category("Loot")]
	public bool FreeForAllLoot { get; set; }

	[Description("The maximum item quality to vendor")]
	[Setting]
	[Category("Loot")]
	[DisplayName("Maximum Vendor Item Quality")]
	[Styx.Helpers.DefaultValue(WoWItemQuality.Uncommon)]
	public WoWItemQuality SellItemQuality { get; set; }

	[Styx.Helpers.DefaultValue(false)]
	[DisplayName("Mail BoE items")]
	[Description("Mails all BOE items that are not vendored to the 'Recipient' in HB settings")]
	[Setting]
	[Category("Loot")]
	public bool MailBoeItems { get; set; }

	[Setting]
	[Styx.Helpers.DefaultValue(WoWItemQuality.Common)]
	[Category("Loot")]
	[DisplayName("Minimum Mail Item Quality")]
	[Description("The minimum boe item quality to mail")]
	public WoWItemQuality MailItemQuality { get; set; }

	[Browsable(false)]
	[Styx.Helpers.DefaultValue(DungeonType.None)]
	[Setting]
	public DungeonType DungeonType { get; set; }

	[Browsable(false)]
	[Setting]
	public uint SelectedRandomDungeonId { get; set; }

	[Styx.Helpers.DefaultValue(RaidType.None)]
	[Browsable(false)]
	[Setting]
	public RaidType RaidType { get; set; }

	[Setting]
	[Browsable(false)]
	[ScaffoldColumn(false)]
	public uint[] SelectedRaidIds { get; set; }

	[Styx.Helpers.DefaultValue(ScenarioType.None)]
	[Setting]
	[Browsable(false)]
	public ScenarioType ScenarioType { get; set; }

	[Browsable(false)]
	[Setting]
	public uint SelectedScenarioId { get; set; }

	[Setting]
	[Browsable(false)]
	[ScaffoldColumn(false)]
	public uint[] SelectedDungeonIds { get; set; }

	[Setting]
	[Browsable(false)]
	[ScaffoldColumn(false)]
	public uint[] SpecifiedScenariosIds { get; set; }

	[DisplayName("Kill Optional Bosses")]
	[Styx.Helpers.DefaultValue(false)]
	[Description("Kill bosses that are not neccesary to complete the dungeon")]
	[Setting]
	[Category("Misc")]
	public bool KillOptionalBosses { get; set; }

	[Setting]
	[Description("Toggles whether to abandon low level quests or not when your questlog is full.")]
	[Styx.Helpers.DefaultValue(true)]
	[Category("Misc")]
	[DisplayName("Abandon Low Level Quests")]
	public bool AbandonLowLevelQuests { get; set; }

	[Styx.Helpers.DefaultValue(true)]
	[Setting]
	[Category("Misc")]
	[DisplayName("Pickup Quests")]
	[Description("Toggles whether to pick up quests. Note: scripts will continue to do quests if they are found in quest log.")]
	public bool PickupQuests { get; set; }

	[Category("Misc")]
	[Description("Toggles whether to pick up hearth when doing a vendor run and current zone isn't same zone that hearth is set to.")]
	[Styx.Helpers.DefaultValue(true)]
	[DisplayName("Hearth on Vendor run")]
	[Setting]
	public bool HearthOnVendorRun { get; set; }

	[Category("Misc")]
	[DisplayName("Following Distance")]
	[Description("Max Distance Before Following Tank (Default : 20)")]
	[Setting]
	[Styx.Helpers.DefaultValue(20)]
	public int FollowingDistance { get; set; }

	[Styx.Helpers.DefaultValue(false)]
	[Setting]
	[Category("Misc")]
	[DisplayName("Combat Movement")]
	[Description("Toggles whether to control movement when in combat or pulling. Requires movement to be disabled in Combat Routine ")]
	public bool CombatMovement { get; set; }

	[Setting]
	[Browsable(false)]
	public bool ShowAllDungeons { get; set; }

	[Setting]
	[Browsable(false)]
	public bool ShowAllScenarios { get; set; }

	[Setting]
	[Styx.Helpers.DefaultValue("")]
	[Description("Path to your custom scripts and profiles folder. Scripts need to be placed in a sub folder. HB restart might be required.")]
	[Editor(typeof(FolderNameEditor), typeof(UITypeEditor))]
	[Category("Advanced")]
	[DisplayName("Custom Scripts Path")]
	public string CustomProfileAndScriptsPath { get; set; }

	[Styx.Helpers.DefaultValue(QueueRole.Auto)]
	[Setting]
	[Description("Specifies which role to queue with. If set to auto then role is automatically chosed based on your current spec")]
	[Category("Advanced")]
	[DisplayName("Role")]
	public QueueRole Role { get; set; }

	[Description("Enables queuing as tank in random LFG. This is unsafe and not recommended. Use at your own risk")]
	[Category("Advanced")]
	[Setting]
	[DisplayName("Tank In Random Groups (UNSAFE)")]
	[Styx.Helpers.DefaultValue(false)]
	public bool TankInRandomGroups { get; set; }

	[Styx.Helpers.DefaultValue(10)]
	[DisplayName("Inside Instance Repair %")]
	[Description("Lowest durability percent before repairing while inside an instance. (Default : 10)")]
	[Setting]
	[Category("Vendor")]
	public float InsideInstanceRepairPercent { get; set; }

	[DisplayName("Outside Instance Repair %")]
	[Category("Vendor")]
	[Description("Lowest durability percent before repairing while not in an instance. (Default : 50)")]
	[Setting]
	[Styx.Helpers.DefaultValue(50)]
	public float OutsideInstanceRepairPercent { get; set; }

	[Styx.Helpers.DefaultValue(3)]
	[DisplayName("Inside Instance Minimum Free Bag Slots")]
	[Description("Bot will port out of dungeon and sell items if number of free bag slots is less than or equal to this value")]
	[Setting]
	[Category("Vendor")]
	public int InsideInstanceMinFreeBagSlots { get; set; }

	[Setting]
	[DisplayName("Minimum Free Bag Slots Outside Instance")]
	[Description("Bot will sell items before entering an instance if number of free bag slots is less than or equal to this value")]
	[Category("Vendor")]
	[Styx.Helpers.DefaultValue(15)]
	public int OutsideInstanceMinFreeBagSlots { get; set; }

	[Description("Toggles if bot should port out during an instance run to sell when bags are nearly full.")]
	[Setting]
	[Styx.Helpers.DefaultValue(true)]
	[Category("Vendor")]
	[DisplayName("Port Out To Sell")]
	public bool PortOutsideInstanceToSell { get; set; }

	[Setting]
	[DisplayName("Port Out To Buy")]
	[Description("Toggles if bot should port out during an instance run to buy drinks when completely out of drinks and is a mana user.")]
	[Styx.Helpers.DefaultValue(true)]
	[Category("Vendor")]
	public bool PortOutsideInstanceToBuy { get; set; }

	static DungeonBuddySettings()
	{
		string text = ((StyxWoW.Me != null) ? StyxWoW.Me.Name : "");
		string returnVal = Lua.GetReturnVal<string>("return GetRealmName()", 0u);
		string path = $"Settings\\DungeonBuddySettings_{text}.xml";
		string path2 = $"Settings\\{returnVal}\\{text}\\DungeonBuddySettings.xml";
		string text2 = Path.Combine(DungeonBot.HbPath, path);
		string text3 = Path.Combine(DungeonBot.HbPath, path2);
		bool flag = File.Exists(text2);
		if (!File.Exists(text3) && flag)
		{
			if (!Directory.Exists(Path.GetDirectoryName(text3)))
			{
				Directory.CreateDirectory(Path.GetDirectoryName(text3));
			}
			File.Copy(text2, text3);
		}
		Instance = new DungeonBuddySettings(text3);
	}

	public DungeonBuddySettings(string path)
		: base(path)
	{
		if (SelectedDungeonIds == null)
		{
			SelectedDungeonIds = new uint[0];
		}
		if (PartyMembers == null)
		{
			PartyMembers = new string[0];
		}
		if (SpecifiedScenariosIds == null)
		{
			SpecifiedScenariosIds = new uint[0];
		}
		if (SelectedRaidIds == null)
		{
			SelectedRaidIds = new uint[0];
		}
	}
}
