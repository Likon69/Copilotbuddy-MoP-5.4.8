using System;
using Levelbot.Resources;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals.WoWObjects;

namespace Levelbot.Actions.Combat
{
    public class ActionSetTarget : Styx.TreeSharp.Action
    {
        protected override RunStatus Run(object context)
        {
            Navigator.Clear();
            LocalPlayer me = StyxWoW.Me;
            if (me.GotTarget && me.CurrentTarget.IsDead)
            {
                me.ClearTarget();
                return RunStatus.Success;
            }

            WoWUnit firstUnit;
            if ((firstUnit = Targeting.Instance.FirstUnit) != null)
            {
                firstUnit.Target();
                try
                {
                    if (firstUnit.IsPlayer)
                    {
                        TreeRoot.StatusText = Utilities.FormatString(LevelbotResources.ActionSetTarget_Run_Setting_level__0___1___2__at__4__yards_as_your_target,
                            firstUnit.Level, firstUnit.Race, firstUnit.Class, Math.Round(firstUnit.Distance));
                    }
                    else if (firstUnit.OwnedByUnit != null)
                    {
                        TreeRoot.StatusText = Utilities.FormatString(LevelbotResources.ActionSetTarget_Run_Setting_level__0___1___2__s_pet_at__4__yards_as_your_target,
                            firstUnit.OwnedByUnit.Level, firstUnit.OwnedByUnit.Race, firstUnit.OwnedByUnit.Class, Math.Round(firstUnit.Distance));
                    }
                    else
                    {
                        TreeRoot.StatusText = Utilities.FormatString(LevelbotResources.ActionSetTarget_Run_Setting__0__at__1__yards_as_your_target,
                            firstUnit.Name, Math.Round(firstUnit.Distance));
                    }
                }
                catch
                {
                    TreeRoot.StatusText = LevelbotResources.ActionSetTarget_Run_Setting_target_;
                }
            }

            return RunStatus.Success;
        }
    }
}
