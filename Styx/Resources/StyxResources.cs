using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace Styx.Resources
{
    [GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [CompilerGenerated]
    [DebuggerNonUserCode]
    public class StyxResources
    {
        internal StyxResources()
        {
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static ResourceManager ResourceManager
        {
            get
            {
                if (object.ReferenceEquals(resourceMan, null))
                {
                    ResourceManager resourceManager = new ResourceManager("Styx.Resources.StyxResources", typeof(StyxResources).Assembly);
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

        public static string CurrentBuildOfWowNotSupported
        {
            get
            {
                return ResourceManager.GetString("CurrentBuildOfWowNotSupported", resourceCulture);
            }
        }

        public static string CurrentBuildNotSupportedException
        {
            get
            {
                return ResourceManager.GetString("CurrentBuildNotSupportedException", resourceCulture);
            }
        }

        public static string Cannot_run_injection_routines_while_not_in_game___Crash_Prevention_
        {
            get
            {
                return ResourceManager.GetString("Cannot_run_injection_routines_while_not_in_game___Crash_Prevention_", resourceCulture);
            }
        }

        private static ResourceManager resourceMan;

        private static CultureInfo resourceCulture;
    }
}
