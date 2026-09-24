using System;
using Levelbot.Resources;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Routines;
using Styx.TreeSharp;
using Styx.WoWInternals.WoWObjects;

namespace Levelbot.Actions.Combat
{
    public class ActionPull : Styx.TreeSharp.Action
    {
        private static LocalPlayer Me
        {
            get { return StyxWoW.Me; }
        }

        protected override RunStatus Run(object context)
        {
            if (Me.CurrentTarget == null)
                return RunStatus.Failure;

            if (Me.Mounted)
            {
                Mount.Dismount("Pulling");
                return RunStatus.Running;
            }

            if (StyxWoW.Me.Combat)
                return RunStatus.Success;

            WoWUnit currentTarget = Me.CurrentTarget;
            string name = Me.CurrentTarget.Name;
            if (Me.CurrentTarget.IsDead)
            {
                Blacklist.Add(Me.CurrentTargetGuid, BlacklistFlags.Pull, TimeSpan.FromMinutes(5.0), "Target is dead");
                Me.ClearTarget();
                return RunStatus.Failure;
            }

            if (currentTarget.TaggedByOther && !currentTarget.TaggedByMe && !Me.GroupInfo.IsInParty && !Me.GroupInfo.IsInRaid)
            {
                Logging.Write(LevelbotResources.ActionPull_Run__0__is_tagged, name);
                Blacklist.Add(Me.CurrentTargetGuid, BlacklistFlags.Pull, TimeSpan.FromSeconds(30.0), "Target is tagged by somebody else");
                Me.ClearTarget();
                return RunStatus.Failure;
            }

            if (RoutineManager.Current.NeedPullBuffs)
            {
                TreeRoot.StatusText = LevelbotResources.ActionPull_Run_Pre_pull_buffs;
                RoutineManager.Current.PullBuff();
            }

            TreeRoot.StatusText = Me.CurrentTarget.IsPlayer
                ? Utilities.FormatString(LevelbotResources.ActionPull_Run_Pulling_level__0___1__now_, Me.CurrentTarget.Level, Me.CurrentTarget.Class, Math.Round(Me.CurrentTarget.Distance))
                : Utilities.FormatString(LevelbotResources.ActionPull_Run_Pulling__0__now_, Me.CurrentTarget.Name, Math.Round(Me.CurrentTarget.Distance));

            RoutineManager.Current.Pull();
            return RunStatus.Success;
        }
    }
}
