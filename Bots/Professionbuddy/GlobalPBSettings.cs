using System.IO;
using Styx.Common;
using Styx.Helpers;

namespace Bots.Professionbuddy
{
	public class GlobalPBSettings : Settings
	{

        public static readonly GlobalPBSettings Instance = new GlobalPBSettings();
        public GlobalPBSettings() : base(Path.Combine(Utilities.AssemblyDirectory, string.Format(@"Settings\{0}\{0}.xml", ProfessionbuddyBot.Instance.Name)))
        {
            Load();
        }

		[Setting, DefaultValue(null)]
		public string DataStoreTable { get; set; }
    }
}