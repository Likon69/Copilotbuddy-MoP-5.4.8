using System.IO;
using Styx;
using Styx.Helpers;

namespace Bots.ArchaeologyBuddy;

public class ArchSettings : Settings
{
	[Setting]
	[DefaultValue(true)]
	public bool AutoSolveArtifacts { get; set; }

	[DefaultValue(false)]
	[Setting]
	public bool UseKeystones { get; set; }

	[Setting]
	[DefaultValue(true)]
	public bool CrateArtifacts { get; set; }

	[Setting]
	[DefaultValue(true)]
	public bool AutoTrain { get; set; }

	[Setting]
	[DefaultValue(375)]
	public int TrainUpto { get; set; }

	public ArchSettings()
		: base(Path.Combine(Path.Combine(Path.Combine(Settings.SettingsDirectory, StyxWoW.Me.RealmName), StyxWoW.Me.Name), "ABSettings.xml"))
	{
		Load();
	}

	~ArchSettings()
	{
		Save();
	}
}
