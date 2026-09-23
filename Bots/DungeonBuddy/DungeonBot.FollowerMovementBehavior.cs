using System;
using System.Linq;
using System.Threading.Tasks;
using Bots.DungeonBuddy.Enums;
using Bots.DungeonBuddy.Helpers;
using Buddy.Coroutines;
using CommonBehaviors.Actions;
using Styx;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.Helpers;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Bots.DungeonBuddy
{
    public partial class DungeonBot
    {
        private const float PIx2 = (float)(Math.PI * 2);

        private readonly WaitTimer _followerLocationTimer = new WaitTimer(TimeSpan.FromSeconds(5));
        private HookExecutor? _followerMovementHook;
        private readonly float _groupFollowerRandomAngleFromCenter = (float)(ScriptHelpers.Rnd.NextDouble() * 6.2831854820251465);
        private readonly float _partyFollowerRandomDistanceFromCenter = (float)ScriptHelpers.Rnd.NextDouble() * 3f;
        private readonly float _raidFollowerRandomDistanceFromCenter = (float)ScriptHelpers.Rnd.NextDouble() * 6f;
        private WoWPoint _lastRaidFollowerLocation;

        private async Task<bool> ExecuteFollowerMovementBehavior()
        {
            if (Me.IsGroupLeader)
                return await PassLeadershipToTank();

            if (_followerMovementHook == null)
            {
                _followerMovementHook = new HookExecutor(
                    "Dungeonbot_FollowerMovement",
                    "Dungeonbuddy's behavior for moving followers while out of combat",
                    new ActionRunCoroutine(context => MoveFollower()));
            }

            return await _followerMovementHook.ExecuteCoroutine();
        }

        private async Task<bool> PassLeadershipToTank()
        {
            if (!Me.IsGroupLeader)
                return false;

            var tank = GroupMember.GroupMembers.FirstOrDefault(m => m.IsTank);
            if (tank == null)
                return false;

            await Coroutine.Sleep(ScriptHelpers.Rnd.Next(1000, 4000));
            Lua.DoString($"PromoteToLeader(\"{tank.Name.ToFormatedUTF8()}\")");
            Logger.Write("Passing group leader to a tank");
            await CommonCoroutines.SleepForLagDuration();
            return true;
        }

        private async Task<bool> MoveFollower()
        {
            WoWPoint destination = WoWPoint.Zero;
            DungeonBuddySettings settings = DungeonBuddySettings.Instance;

            if (Me.CurrentMap.IsRaid &&
                (settings.PartyMode != PartyMode.Follower ||
                 !string.IsNullOrEmpty(settings.LeaderName) &&
                 !GroupMember.GroupMembers.Any(member => member.NameEquals(settings.LeaderName))))
            {
                destination = GetMoveToRaidLocation();
            }

            if (destination == WoWPoint.Zero)
            {
                destination = GetMoveToLeaderLocation();
            }

            if (destination != WoWPoint.Zero && !Navigator.AtLocation(destination))
            {
                await CommonCoroutines.MoveTo(destination);
                return true;
            }

            return false;
        }

        private WoWPoint GetMoveToRaidLocation()
        {
            WoWPoint groupCenterLocation = ScriptHelpers.GetGroupCenterLocation(null, 30f);
            WoWPoint result = GetFollowerRandomLocation(groupCenterLocation);
            _ = Me.Location;
            if (_lastRaidFollowerLocation != WoWPoint.Zero &&
                (result.DistanceSqr(_lastRaidFollowerLocation) < 100f ||
                 (!_followerLocationTimer.IsFinished && result.DistanceSqr(_lastRaidFollowerLocation) > 2500f)))
            {
                result = _lastRaidFollowerLocation;
            }
            else
            {
                _lastRaidFollowerLocation = result;
                _followerLocationTimer.Reset();
            }

            return result;
        }

        private WoWPoint GetMoveToLeaderLocation()
        {
            GroupMember? leader = GroupMember.GroupMembers
                .Where(member => member.IsLeader())
                .OrderByDescending(member => member.MaxHealth)
                .FirstOrDefault();

            if (leader == null || leader.MapId != Me.MapId)
            {
                return WoWPoint.Zero;
            }

            WoWPlayer? player = leader.Player;
            WoWPoint myLocation = Me.Location;
            WoWPoint leaderLocation = leader.Location;
            bool leaderInLineOfSight = player != null && player.InLineOfSpellSight;
            float distance = myLocation.Distance(leaderLocation);

            if ((leaderInLineOfSight || player == null) && distance < DungeonBuddySettings.Instance.FollowingDistance)
            {
                if (Me.IsMoving && distance < DungeonBuddySettings.Instance.FollowingDistance - DungeonNavigator.PathPrecision)
                {
                    WoWMovement.MoveStop();
                }

                return WoWPoint.Zero;
            }

            if (!Navigator.CanNavigateFully(myLocation, leaderLocation) &&
                leaderLocation.DistanceSqr(myLocation) < 10000f &&
                Math.Abs(leaderLocation.Z - myLocation.Z) < 30f)
            {
                Navigator.PlayerMover.MoveTowards(leaderLocation);
            }

            return myLocation.DistanceSqr(leaderLocation) > DungeonNavigator.PathPrecisionSqr
                ? leaderLocation
                : WoWPoint.Zero;
        }

        private WoWPoint GetFollowerRandomLocation(WoWPoint centerLocation)
        {
            float distance = Me.GroupInfo.IsInRaid
                ? _raidFollowerRandomDistanceFromCenter
                : _partyFollowerRandomDistanceFromCenter;

            for (float relativeAngle = 0f; relativeAngle < 360f; relativeAngle += 20f)
            {
                float angle = _groupFollowerRandomAngleFromCenter + WoWMathHelper.DegreesToRadians(relativeAngle);
                angle = WoWMathHelper.NormalizeRadian(angle);
                WoWPoint randomPoint = centerLocation.RayCast(angle, distance);
                var heights = Navigator.FindHeights(randomPoint.X, randomPoint.Y);
                if (heights == null || !heights.Any())
                {
                    continue;
                }

                randomPoint.Z = heights.OrderBy(height => Math.Abs(centerLocation.Z - height)).FirstOrDefault();
                if (Avoidance.Helpers.MeshTraceline(Me.Location, randomPoint) == false)
                {
                    return randomPoint;
                }
            }

            var centerHeights = Navigator.FindHeights(centerLocation.X, centerLocation.Y);
            if (centerHeights != null && centerHeights.Any())
            {
                centerLocation.Z = centerHeights.OrderBy(height => Math.Abs(centerLocation.Z - height)).FirstOrDefault();
                if (Navigator.CanNavigateFully(Me.Location, centerLocation))
                {
                    return centerLocation;
                }
            }

            return centerLocation;
        }
    }
}
