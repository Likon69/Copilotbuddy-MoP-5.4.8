using System;
using Levelbot.Resources;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Routines;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Bots.Quest.Actions.Combat
{
    public class ActionPull : Styx.TreeSharp.Action
    {
        private static LocalPlayer Me { get { return StyxWoW.Me; } }

        #region Overrides of Behavior

        /// <summary>
        /// Actually executes one behavior tick
        /// </summary>
        /// <returns></returns>
        protected override RunStatus Run(object context)
        {            
            if (Me.CurrentTarget == null)
                return RunStatus.Failure;
            
            if (Me.Mounted)
            {
                if (Me.IsFlying && Me.CurrentTarget != null && Me.CurrentTarget.Distance > 3 && !Me.IsMoving)
                {
                    return RunStatus.Running;
                }

                if (Me.IsFlying && Me.CurrentTarget != null && Me.CurrentTarget.Distance <= 3)
                {
                    Flightor.MountHelper.Dismount();
                    return RunStatus.Running;
                }

                Flightor.MountHelper.Dismount();
                return RunStatus.Running;
            }

            if (!StyxWoW.Me.Combat)
            {
                WoWUnit unit = Me.CurrentTarget;
                string targetName = Me.CurrentTarget.Name;

                if (Me.CurrentTarget.IsDead)
                {
                    Blacklist.Add(Me.CurrentTarget.CurrentTargetGuid, BlacklistFlags.Pull, TimeSpan.FromMinutes(5), "Target is dead!");
                    Me.ClearTarget();
                    return RunStatus.Failure;
                }

                if (unit.TaggedByOther && !unit.TaggedByMe && !(Me.GroupInfo.IsInParty || Me.GroupInfo.IsInRaid))
                {
                    Logging.Write(LevelbotResources.ActionPull_Run__0__is_tagged, targetName);
                    Blacklist.Add(Me.CurrentTargetGuid, BlacklistFlags.Pull, TimeSpan.FromMinutes(5), "Target is tagged by somebody else");
                    Me.ClearTarget();
                    return RunStatus.Failure;
                }

                if (RoutineManager.Current.NeedPullBuffs)
                {
                    TreeRoot.StatusText = LevelbotResources.ActionPull_Run_Pre_pull_buffs;
                    RoutineManager.Current.PullBuff();
                }

                TreeRoot.StatusText = Me.CurrentTarget.IsPlayer ? Utilities.FormatString(LevelbotResources.ActionPull_Run_Pulling_level__0___1__now_, 
                    Me.CurrentTarget.Level, Me.CurrentTarget.Class, Math.Round(Me.CurrentTarget.Distance)) : 
                    Utilities.FormatString(LevelbotResources.ActionPull_Run_Pulling__0__now_, Me.CurrentTarget.Name, Math.Round(Me.CurrentTarget.Distance));
                
                RoutineManager.Current.Pull();
                return RunStatus.Success;
            }

            return RunStatus.Success;
        }

        #endregion
    }
}
