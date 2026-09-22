using System;

using Levelbot.Resources;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Bots.Quest.Actions.Combat
{
    public class ActionSetTarget : Styx.TreeSharp.Action
    {
        #region Overrides of Behavior

        /// <summary>
        /// Actually executes one behavior tick
        /// </summary>
        /// <returns></returns>
        protected override RunStatus Run(object context)
        {
            Navigator.Clear();
            LocalPlayer me = StyxWoW.Me;
            if (me.GotTarget && me.CurrentTarget.IsDead)
            {
                me.ClearTarget();
                return RunStatus.Success;
            }

            WoWUnit target;
            if ((target = Targeting.Instance.FirstUnit) != null)
            {
                target.Target();

                try
                {
                    if (target.IsPlayer)
                    {

                        TreeRoot.StatusText =
                            Utilities.FormatString(LevelbotResources.ActionSetTarget_Run_Setting_level__0___1___2__at__4__yards_as_your_target,
                                                   target.Level, target.Race, target.Class,
                                                   Math.Round(target.Distance)
                                );
                    }
                    else if (target.OwnedByUnit != null)
                    {
                        TreeRoot.StatusText =
                            Utilities.FormatString(LevelbotResources.ActionSetTarget_Run_Setting_level__0___1___2__s_pet_at__4__yards_as_your_target,
                                                   target.OwnedByUnit.Level, target.OwnedByUnit.Race, target.OwnedByUnit.Class,
                                                   Math.Round(target.Distance)
                                );
                    }
                    else
                    {
                        TreeRoot.StatusText = Utilities.FormatString(LevelbotResources.ActionSetTarget_Run_Setting__0__at__1__yards_as_your_target,
                                                                    target.Name,
                                                                    Math.Round(target.Distance)
                            );
                    }
                }
                catch
                {
                    TreeRoot.StatusText = LevelbotResources.ActionSetTarget_Run_Setting_target_;
                }
            }

            if (StyxWoW.Me.Mounted)
            {
                Logging.WriteDiagnostic(LevelbotResources.ActionSetTarget_Run_Dismounting_for_combat_);
                Mount.Dismount("SetTarget");
            }

            return RunStatus.Success;
        }

        #endregion
    }
}
