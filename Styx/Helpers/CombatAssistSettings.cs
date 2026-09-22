using System.IO;

namespace Styx.Helpers
{
    public class CombatAssistSettings : Settings
    {
        public CombatAssistSettings()
            : base(Path.Combine(SettingsDirectory, StyxWoW.Me.RealmName, StyxWoW.Me.Name, "CombatAssistSettings.xml"))
        {
        }

        [DefaultValue(3)]
        [Setting]
        public int RafFollowDistance { get; set; }

        public static readonly CombatAssistSettings Instance = new CombatAssistSettings();
    }
}
