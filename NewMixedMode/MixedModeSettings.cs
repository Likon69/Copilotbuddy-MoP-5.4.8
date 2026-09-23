using System.IO;
using Styx;
using Styx.Helpers;

namespace NewMixedMode;

public class MixedModeSettings : Settings
{
	[Setting]
	public string PrimaryBot { get; set; }

	[Setting]
	public string SecondaryBot { get; set; }

	public MixedModeSettings()
		: base(Path.Combine(Path.Combine(Path.Combine(Settings.SettingsDirectory, StyxWoW.Me.RealmName), StyxWoW.Me.Name), "MixedModeSettings.xml"))
	{
	}
}
