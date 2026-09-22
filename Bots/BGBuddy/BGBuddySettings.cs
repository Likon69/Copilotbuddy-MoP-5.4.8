using System.ComponentModel;
using System.IO;

using Styx;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.Helpers;
using Styx.WoWInternals;

namespace Bots.BGBuddy
{
    class BGBuddySettings : Settings
    {
        public BGBuddySettings()
            : base(Path.Combine(Path.Combine(Path.Combine(SettingsDirectory, StyxWoW.Me.RealmName), StyxWoW.Me.Name), "BGBuddySettings.xml"))
        {
        }

        private static BGBuddySettings _inst;
        public static BGBuddySettings Instance { get { return _inst ?? (_inst = new BGBuddySettings()); } }

        [Setting]
        [Browsable(false)]
        [Styx.Helpers.DefaultValue(true)]
        public bool FirstTime { get; set; }

        [Setting]
        [Category("Settings")]
        [DisplayName("Loot Corpses")]
        [Description("Enabling this will make the toon loot player corpses if there is nothing else to do")]
        [Styx.Helpers.DefaultValue(false)]
        public bool LootCorpses { get; set; }

        [Setting]
        [Category("Settings")]
        [DisplayName("Pull Distance")]
        [Description("That is the distance that CC's pull and pullbuff behavior will be called. Suggested to be lowered for melee chars, so they don't dismount far away")]
        [Styx.Helpers.DefaultValue(40.0d)]
        public double PullDistance { get; set; }

        private bool _disableMounting;

        [Setting]
        [Category("Settings")]
        [DisplayName("Disable Mounting")]
        [Styx.Helpers.DefaultValue(false)]
        public bool DisableMounting
        {
            get { return _disableMounting; }
            set
            {
                _disableMounting = value;
                if (ProfileManager.CurrentProfile != null)
                {
                    ProfileManager.CurrentProfile.UseMount = _disableMounting ? false : (bool?)null;
                }
            }
        }

        [Setting]
        [Category("Queue")]
        [DisplayName("Queue #1")]
        [Description("First battleground to queue")]
        public BattlegroundType Queue1 { get; set; }

        [Setting]
        [Category("Queue")]
        [DisplayName("Queue #2")]
        [Description("Second battleground to queue")]
        public BattlegroundType Queue2 { get; set; }

        [Setting]
        [Browsable(false)]
        public LogicType EotsLogicType { get; set; }

        [Setting]
        [Browsable(false)]
        public LogicType BfgLogicType { get; set; }

        [Setting]
        [Browsable(false)]
        public LogicType AVLogicType { get; set; }

        [Setting]
        [Browsable(false)]
        public LogicType WsgLogicType { get; set; }

        [Setting]
        [Browsable(false)]
        public LogicType TpLogicType { get; set; }

        [Setting]
        [Browsable(false)]
        public LogicType IocLogicType { get; set; }

        [Setting]
        [Browsable(false)]
        public LogicType SotaLogicType { get; set; }

        [Setting]
        [Browsable(false)]
        public LogicType AbLogicType { get; set; }

        [Setting]
        [Browsable(false)]
        public LogicType SMLogicType { get; set; }

        [Setting]
        [Browsable(false)]
        public LogicType TOKLogicType { get; set; }
    }
}
