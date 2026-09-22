using System;
using CommonBehaviors.Actions;
using Levelbot.Resources;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.POI;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals.WoWObjects;

namespace Levelbot.Actions.Combat
{
    public class ActionMoveToTarget : NavigationAction
    {
        private int _tickCount;

        public ActionMoveToTarget()
            : base(null, null)
        {
            BotEvents.Player.OnMobKilled += Player_OnMobKilled;
        }

        private void Player_OnMobKilled(BotEvents.Player.MobKilledEventArgs args)
        {
            _tickCount = 0;
        }

        protected override RunStatus Run(object context)
        {
            if (_tickCount == 0)
                _tickCount = Environment.TickCount;

            WoWUnit target = (BotPoi.Current.AsObject as WoWUnit) ?? Targeting.Instance.FirstUnit;
            if (target == null)
                return RunStatus.Failure;

            if (Environment.TickCount - _tickCount >= 45000)
            {
                _tickCount = 0;
                TimeSpan blacklistTime = TimeSpan.FromMinutes(10.0);
                if (target is WoWPlayer)
                    blacklistTime = TimeSpan.FromSeconds(45.0);

                Blacklist.Add(target.Guid, BlacklistFlags.Pull, blacklistTime, "Tried to move to target but couldn't reach");
                StyxWoW.Me.ClearTarget();
                Logging.Write(LevelbotResources.ActionMoveToTarget_Run_Tried_to_move_to__0__for_45_seconds__blacklisting_, target.Name);
                return RunStatus.Failure;
            }

            if (target.Location.Distance(StyxWoW.Me.Location) <= Targeting.PullDistance && target.InLineOfSpellSight)
            {
                _tickCount = 0;
                Navigator.Clear();
                return RunStatus.Success;
            }

            WoWPoint end = target.Location;
            WoWPoint[] points = Navigator.GeneratePath(StyxWoW.Me.Location, end);
            if (points.Length == 0 || !AtLocation(points[points.Length - 1], end))
            {
                _tickCount = 0;
                TimeSpan blacklistTime = TimeSpan.FromMinutes(10.0);
                if (target is WoWPlayer)
                    blacklistTime = TimeSpan.FromSeconds(45.0);

                Blacklist.Add(target.Guid, BlacklistFlags.Pull, blacklistTime, "Couldn't generate a path to the target");
                StyxWoW.Me.ClearTarget();
                Logging.Write(LevelbotResources.ActionMoveToTarget_Run_MoveToTarget__Could_not_generate_path_to_target___0___blacklisting_, target.Name);
                return RunStatus.Failure;
            }

            if (target.Type == WoWObjectType.Player)
                TreeRoot.StatusText = Utilities.FormatString(LevelbotResources.ActionMoveToTarget_Run_Moving_towards_level__0___1___2_, target.Level, target.Race, target.Class);
            else
                TreeRoot.StatusText = LevelbotResources.ActionMoveToTarget_Run_Moving_towards_ + target.Name;

            if (StyxWoW.Me.IsFlying)
            {
                Flightor.MoveTo(target.Location, 40f, true);
                return RunStatus.Success;
            }

            return Navigator.GetRunStatusFromMoveResult(Navigator.MoveTo(target.Location));
        }

        private static bool AtLocation(WoWPoint from, WoWPoint to)
        {
            return from.Distance2DSqr(to) <= Navigator.PathPrecision * Navigator.PathPrecision && Math.Abs(from.Z - to.Z) < 3f;
        }
    }
}
