using System.IO;
using Styx.WoWInternals;

namespace Styx.Helpers
{
    public class PVPSettings : Settings
    {
        public PVPSettings()
            : base(Path.Combine(SettingsDirectory, StyxWoW.Me.RealmName, StyxWoW.Me.Name, "PVPSettings.xml"))
        {
        }

        [DefaultValue(BattlegroundType.WSG)]
        [Setting("BG1", "The first BG you would like to queue for.")]
        public BattlegroundType BG1 { get; set; }

        [Setting("BG2", "The second BG you would like to queue for.")]
        [DefaultValue(BattlegroundType.None)]
        public BattlegroundType BG2 { get; set; }

        [DefaultValue(false)]
        [Setting("IncludeMountedPlayers", "Set this to true if you wish to include mounted players in battlegrounds.")]
        public bool IncludeMountedPlayers { get; set; }

        public static readonly PVPSettings Instance = new PVPSettings();
    }
}
