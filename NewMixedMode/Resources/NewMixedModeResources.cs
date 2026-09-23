using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace NewMixedMode.Resources
{
    [GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [CompilerGenerated]
    [DebuggerNonUserCode]
    public class NewMixedModeResources
    {
        private static ResourceManager _resourceManager;
        private static CultureInfo _culture;

        internal NewMixedModeResources()
        {
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static ResourceManager ResourceManager
        {
            get
            {
                if (ReferenceEquals(_resourceManager, null))
                {
                    _resourceManager = new ResourceManager(
                        "NewMixedMode.Resources.NewMixedModeResources",
                        typeof(NewMixedModeResources).Assembly);
                }

                return _resourceManager;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static CultureInfo Culture
        {
            get { return _culture; }
            set { _culture = value; }
        }

        public static string BotName
        {
            get { return ResourceManager.GetString("BotName", _culture); }
        }

        public static string MixedModeEx_CurrentBot_Switching_bot_to_
        {
            get { return ResourceManager.GetString("MixedModeEx_CurrentBot_Switching_bot_to_", _culture); }
        }

        public static string MixedModeEx_Start_You_have_not_selected_a_primary_bot_
        {
            get { return ResourceManager.GetString("MixedModeEx_Start_You_have_not_selected_a_primary_bot_", _culture); }
        }

        public static string MixedModeEx_Start_You_have_not_selected_a_secondary_bot_
        {
            get { return ResourceManager.GetString("MixedModeEx_Start_You_have_not_selected_a_secondary_bot_", _culture); }
        }
    }
}
