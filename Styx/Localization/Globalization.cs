using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace Styx.Localization
{
    [CompilerGenerated]
    [DebuggerNonUserCode]
    [GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    public class Globalization
    {
        internal Globalization()
        {
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static ResourceManager ResourceManager
        {
            get
            {
                if (object.ReferenceEquals(resourceMan, null))
                {
                    ResourceManager resourceManager = new ResourceManager("Styx.Localization.Globalization", typeof(Globalization).Assembly);
                    resourceMan = resourceManager;
                }
                return resourceMan;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static CultureInfo Culture
        {
            get
            {
                return resourceCulture;
            }
            set
            {
                resourceCulture = value;
            }
        }

        public static string MainWindowTitleFormat
        {
            get
            {
                return ResourceManager.GetString("MainWindowTitleFormat", resourceCulture);
            }
        }

        public static string Honorbuddy_Startup_Complete
        {
            get
            {
                return ResourceManager.GetString("Honorbuddy_Startup_Complete", resourceCulture);
            }
        }

        public static string StartUpError64Bit
        {
            get
            {
                return ResourceManager.GetString("StartUpError64Bit", resourceCulture);
            }
        }

        public static string StartUpErrorNotInGame
        {
            get
            {
                return ResourceManager.GetString("StartUpErrorNotInGame", resourceCulture);
            }
        }

        public static string ProcessTitle
        {
            get
            {
                return ResourceManager.GetString("ProcessTitle", resourceCulture);
            }
        }

        private static ResourceManager resourceMan;

        private static CultureInfo resourceCulture;
    }
}
