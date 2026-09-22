using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace CommonBehaviors.Resources
{
    [GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [CompilerGenerated]
    [DebuggerNonUserCode]
    public class CommonBehaviorsResources
    {
        private static ResourceManager _resourceManager;
        private static CultureInfo _culture;

        internal CommonBehaviorsResources()
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
                        "CommonBehaviors.Resources.CommonBehaviorsResources",
                        typeof(CommonBehaviorsResources).Assembly);
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

        public static string ActionInteract_Run_Interacting_with_
        {
            get { return ResourceManager.GetString("ActionInteract_Run_Interacting_with_", _culture); }
        }

        public static string ActionInteract_Run_Trying_to_interact_with_a_null_object__Wtf_
        {
            get { return ResourceManager.GetString("ActionInteract_Run_Trying_to_interact_with_a_null_object__Wtf_", _culture); }
        }

        public static string ActionMoveToPoi_Run_I_don_t_want_to_move_to__0__0__0_
        {
            get { return ResourceManager.GetString("ActionMoveToPoi_Run_I_don_t_want_to_move_to__0__0__0_", _culture); }
        }

        public static string ActionMoveToPoi_Run_Moving_to_
        {
            get { return ResourceManager.GetString("ActionMoveToPoi_Run_Moving_to_", _culture); }
        }

        public static string ActionSetPoi_Run_Error__
        {
            get { return ResourceManager.GetString("ActionSetPoi_Run_Error__", _culture); }
        }

        public static string ActionWaitForLuaEvent_Run__wait_timed_out_
        {
            get { return ResourceManager.GetString("ActionWaitForLuaEvent_Run__wait_timed_out_", _culture); }
        }

        public static string GalvandarIsDead
        {
            get { return ResourceManager.GetString("GalvandarIsDead", _culture); }
        }

        public static string NothingElseToDoWaitingTower
        {
            get { return ResourceManager.GetString("NothingElseToDoWaitingTower", _culture); }
        }
    }
}
