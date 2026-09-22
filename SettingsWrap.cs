using System.Collections.Generic;
using Styx.Helpers;

namespace CopilotBuddy
{
    public class SettingsWrap
    {
        public CharacterSettings CharSettings { get; set; }
        public GlobalSettings StyxSettings { get; set; }
        public List<string> GroundMounts { get; set; }
        public List<string> FlyingMounts { get; set; }
    }
}
