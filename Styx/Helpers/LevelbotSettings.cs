using System.IO;

namespace Styx.Helpers
{
    public class LevelbotSettings : Settings
    {
        public LevelbotSettings()
            : base(Path.Combine(SettingsDirectory, StyxWoW.Me.RealmName, StyxWoW.Me.Name, "LevelbotSettings.xml"))
        {
        }

        [Setting]
        [DefaultValue(false)]
        public bool GroundMountFarmingMode { get; set; }

        public static readonly LevelbotSettings Instance = new LevelbotSettings();
    }
}
