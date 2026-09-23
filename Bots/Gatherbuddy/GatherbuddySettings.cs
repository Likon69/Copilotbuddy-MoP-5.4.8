using System.IO;
using Styx;
using Styx.Helpers;

namespace Bots.Gatherbuddy;

public class GatherbuddySettings : Settings
{
	private static GatherbuddySettings _instance;
	public static GatherbuddySettings Instance => _instance ?? (_instance = new GatherbuddySettings());

	[Setting, DefaultValue(true)] public bool GatherHerbs { get; set; }
	[Setting, DefaultValue(true)] public bool GatherMinerals { get; set; }
	[Setting, DefaultValue(PathType.Circle)] public PathType PathingType { get; set; }
	[Setting, DefaultValue(false)] public bool UseSpiritHealer { get; set; }
	[Setting, DefaultValue(false)] public bool UseGuildVault { get; set; }
	[Setting, DefaultValue(true)] public bool HearthAndExit { get; set; }
	[Setting, DefaultValue(false)] public bool MailToAlt { get; set; }
	[Setting, DefaultValue(true)] public bool WaitRezSickness { get; set; }
	[Setting, DefaultValue(0)] public float HeightModifier { get; set; }
	[Setting, DefaultValue(0)] public float BottingHours { get; set; }
	[Setting, DefaultValue(true)] public bool NoNinja { get; set; }
	[Setting, DefaultValue(true)] public bool IgnoreElites { get; set; }
	[Setting, DefaultValue(20)] public int BlacklistTimer { get; set; }
	[Setting, DefaultValue(false)] public bool LootMobs { get; set; }
	[Setting, DefaultValue(false)] public bool RepairFromGuildBank { get; set; }
	[Setting, DefaultValue(false)] public bool RandomizeHotspots { get; set; }
	[Setting, DefaultValue(1)] public double RandomizationFactor { get; set; }
	[Setting, DefaultValue(true)] public bool FaceNodes { get; set; }
	[Setting, DefaultValue(false)] public bool DiagnosticLogging { get; set; }

	public GatherbuddySettings()
		: base(Path.Combine(Path.Combine(Path.Combine(Settings.SettingsDirectory, StyxWoW.Me.RealmName), StyxWoW.Me.Name), "GatherbuddySettings.xml"))
	{
		Load();
	}

	~GatherbuddySettings()
	{
		Save();
	}
}
