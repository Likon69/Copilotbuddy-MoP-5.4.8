using System.IO;
using Styx.WoWInternals;

namespace Styx.Helpers
{
    public class BGBotSettings : Settings
    {
        public BGBotSettings()
            : base(Path.Combine(SettingsDirectory, StyxWoW.Me.RealmName, StyxWoW.Me.Name, "BGBotSettings.xml"))
        {
        }

        [Setting("BG1", "The first BG you would like to queue for.")]
        [DefaultValue(BattlegroundType.WSG)]
        public BattlegroundType BG1 { get; set; }

        [Setting("BG2", "The second BG you would like to queue for.")]
        [DefaultValue(BattlegroundType.None)]
        public BattlegroundType BG2 { get; set; }

        [DefaultValue(false)]
        [Setting("IncludeMountedPlayers", "Set this to true if you wish to include mounted players in battlegrounds.")]
        public bool IncludeMountedPlayers { get; set; }

        [DefaultValue(false)]
        [Setting("IsHealer", "Set this to true if you wish to only heal people and not target any players.")]
        public bool IsHealer { get; set; }

        public static readonly BGBotSettings Instance = new BGBotSettings();
    }
}
