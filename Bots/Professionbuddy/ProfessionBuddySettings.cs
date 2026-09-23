using System.IO;
using Styx;
using Styx.Common;
using Styx.Helpers;
using Styx.WoWInternals;

namespace Bots.Professionbuddy
{
	public class ProfessionBuddySettings : Settings
	{
		public static readonly ProfessionBuddySettings Instance;

		[DefaultValue("")]
		[Setting]
		public string LastProfile { get; set; }

		[Setting]
		[DefaultValue("")]
		public string LastBotBase { get; set; }

		static ProfessionBuddySettings()
		{
			string arg = (StyxWoW.Me != null) ? StyxWoW.Me.Name : "";
			string returnVal = Lua.GetReturnVal<string>("return GetRealmName()", 0u);
			string settingsPath = Path.Combine(Utilities.AssemblyDirectory, string.Format("Settings\\{0}\\{0}[{1}-{2}].xml", "ProfessionBuddy", arg, returnVal));
			Instance = new ProfessionBuddySettings(settingsPath);
		}

		private ProfessionBuddySettings(string settingsPath)
			: base(settingsPath)
		{
		}
	}
}
