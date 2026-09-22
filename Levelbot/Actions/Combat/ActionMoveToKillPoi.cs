using System;
using Levelbot.Resources;
using Styx;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.POI;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals.WoWObjects;

namespace Levelbot.Actions.Combat
{
    public class ActionMoveToKillPoi : Styx.TreeSharp.Action
    {
        private readonly WaitTimer _waitTimer = new WaitTimer(TimeSpan.FromSeconds(30.0));
        private ulong _guid;

        public override void Start(object context)
        {
            base.Start(context);

            BotPoi poi = BotPoi.Current;
            if (poi.Type != PoiType.Kill)
                return;

            if (_guid != poi.Guid)
            {
                _waitTimer.Reset();
                _guid = poi.Guid;
            }

            if (!Navigator.CanNavigateFully(StyxWoW.Me.Location, poi.Location))
            {
                Blacklist.Add(poi.Guid, BlacklistFlags.Pull, TimeSpan.FromMinutes(10.0), "Couldn't generate a path to the kill poi");
                StyxWoW.Me.ClearTarget();
                BotPoi.Clear("No path to kill poi");
                Logging.Write(LevelbotResources.ActionMoveToTarget_Run_MoveToTarget__Could_not_generate_path_to_target___0___blacklisting_, poi.Name);
            }
        }

        protected override RunStatus Run(object context)
        {
            BotPoi poi = BotPoi.Current;
            if (poi.Type != PoiType.Kill)
                return RunStatus.Failure;

            if (_waitTimer.IsFinished)
            {
                Blacklist.Add(poi.Guid, BlacklistFlags.Pull, TimeSpan.FromMinutes(10.0), "Tried to move to target but couldn't reach");
                StyxWoW.Me.ClearTarget();
                BotPoi.Clear("Couldn't reach poi");
                Logging.Write(LevelbotResources.ActionMoveToTarget_Run_Tried_to_move_to__0__for_45_seconds__blacklisting_, poi.Name);
                return RunStatus.Success;
            }

            WoWUnit unit = poi.AsObject as WoWUnit;
            WoWPoint location = StyxWoW.Me.Location;
            if (unit == null)
            {
                if (poi.Location.DistanceSqr(location) < 4900f)
                {
                    Blacklist.Add(poi.Guid, BlacklistFlags.Pull, TimeSpan.FromMinutes(10.0), "Kill poi is invalidated.");
                    StyxWoW.Me.ClearTarget();
                    BotPoi.Clear("Kill poi invalidated");
                    return RunStatus.Success;
                }
            }
            else if (unit.Location.DistanceSqr(location) < Targeting.PullDistanceSqr && unit.InLineOfSpellSight)
            {
                return RunStatus.Success;
            }

            return Navigator.GetRunStatusFromMoveResult(Navigator.MoveTo(poi.Location));
        }
    }
}
