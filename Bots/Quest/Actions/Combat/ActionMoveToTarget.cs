using System;
using Levelbot.Resources;
using Styx;
using CommonBehaviors.Actions;
using Styx.Common;
using Styx.CommonBot;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Bots.Quest.Actions.Combat
{
    public class ActionMoveToTarget : NavigationAction
    {
        public ActionMoveToTarget()
        {
            BotEvents.Player.OnMobKilled += Player_OnMobKilled;    
        }

        void Player_OnMobKilled(BotEvents.Player.MobKilledEventArgs args)
        {
            _tickCount = 0;
        }

        #region Overrides of Behavior

        private int _tickCount;

        /// <summary>
        /// Actually executes one behavior tick
        /// </summary>
        /// <returns></returns>
        protected override RunStatus Run(object context)
        {

            var cachedObject = StyxWoW.Me.CurrentTarget ?? Targeting.Instance.FirstUnit;

            //Logging.WriteDiagnostic("move to the target started");
            if (_tickCount == 0)
                _tickCount = Environment.TickCount;

            if (cachedObject == null)
                return RunStatus.Failure;

            if (Environment.TickCount - _tickCount >= 45000)
            {
                _tickCount = 0;
                TimeSpan blacklistTime = TimeSpan.FromMinutes(10);
                if (cachedObject is WoWPlayer)
                    blacklistTime = TimeSpan.FromSeconds(45);
                Blacklist.Add(cachedObject.Guid, BlacklistFlags.Pull, blacklistTime, "Can't reach the target after 45 seconds!");
                StyxWoW.Me.ClearTarget();
                Logging.Write(LevelbotResources.ActionMoveToTarget_Run_Tried_to_move_to__0__for_45_seconds__blacklisting_, cachedObject.Name);
                return RunStatus.Failure;
            }

            if (cachedObject.Location.Distance(StyxWoW.Me.Location) <= Targeting.PullDistance &&
                cachedObject.InLineOfSpellSight)
            {
				_tickCount = 0;
				Navigator.Clear();
				return RunStatus.Success;
            }

            if (cachedObject.Type == WoWObjectType.Player)
                TreeRoot.StatusText = Utilities.FormatString(LevelbotResources.ActionMoveToTarget_Run_Moving_towards_level__0___1___2_, cachedObject.Level, cachedObject.Race, cachedObject.Class);
            
            else
                TreeRoot.StatusText = LevelbotResources.ActionMoveToTarget_Run_Moving_towards_ + cachedObject.Name;
            
            if (StyxWoW.Me.IsFlying)
			{
				Flightor.MoveTo(cachedObject.Location);
				return RunStatus.Success;
			}

            return Navigator.GetRunStatusFromMoveResult(Navigator.MoveTo(cachedObject.Location));
        }

        private static bool AtLocation(WoWPoint myPos, WoWPoint loc)
        {
            if (myPos.Distance2DSqr(loc) > Navigator.PathPrecision * Navigator.PathPrecision)
                return false;

            return Math.Abs(myPos.Z - loc.Z) < 3f;
        }

        #endregion
    }
}
