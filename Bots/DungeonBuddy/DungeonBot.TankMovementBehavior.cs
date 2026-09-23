using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Buddy.Coroutines;
using CommonBehaviors.Actions;
using CommonBehaviors.Decorators;
using Styx;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.POI;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Styx.TreeSharp;
using Tripper.Navigation;
using Action = Styx.TreeSharp.Action;
using Bots.DungeonBuddy.Helpers;
using Bots.DungeonBuddy.Profiles.Handlers;
using Bots.DungeonBuddy.Enums;

namespace Bots.DungeonBuddy
{
    public partial class DungeonBot
    {
        private static GroupInfoMisc _partyMoveToInfo;
        private static PathFindResult _pathResult;
        private static readonly WaitTimer _pathToBossTimer = new WaitTimer(TimeSpan.FromSeconds(8));

        private readonly WaitTimer _manaRegenCycleTimer = WaitTimer.ThirtySeconds;
        private readonly WaitTimer _manaRegenWindowTimer = new WaitTimer(TimeSpan.FromSeconds(7));

        private Composite _tankWaitBehavior;
        private HookExecutor _tankMovementExecutor;

        private static int MaxFollowRangeSqr
        {
            get { return Me.Mounted ? MaxUnmountedFollowerRangeSqr : MaxMountedFollowerRangeSqr; }
        }

        private bool ManaRegenTimerIsRunning
        {
            get
            {
                if (_manaRegenCycleTimer.IsFinished)
                {
                    _manaRegenCycleTimer.Reset();
                    _manaRegenWindowTimer.Reset();
                }
                return !_manaRegenWindowTimer.IsFinished;
            }
        }

        private Composite TankWaitBehavior
        {
            get { return _tankWaitBehavior ?? (_tankWaitBehavior = CreateTankWaitBehavior()); }
        }

        private async Task<bool> ExecuteTankBehavior()
        {
            _partyMoveToInfo = GroupInfoMisc.GetPartyMovetoInfo();

            var target = Targeting.Instance.FirstUnit;
            var isEngagingTarget = target != null && (target.ThreatInfo.ThreatStatus > ThreatStatus.UnitNotInThreatTable || target.IsTargetingMyRaidMember);

            if (!isEngagingTarget && await TankWaitBehavior.ExecuteCoroutine())
                return true;

            var needsRegroup = _partyMoveToInfo.DeadGroupMembers.Any() || _partyMoveToInfo.DistantGroupLoc != WoWPoint.Zero;
            var poiNotKill = BotPoi.Current.Type != PoiType.Kill;
            var targetAlive = target != null && target.IsAlive;

            if (poiNotKill && (isEngagingTarget || (targetAlive && !needsRegroup)))
            {
                BotPoi.Current = new BotPoi(target, PoiType.Kill);
                return true;
            }

            if (needsRegroup && !isEngagingTarget)
            {
                if (await MoveToDeadPartyMember(_partyMoveToInfo.DeadGroupMembers))
                    return true;
                if (await MoveToDistantGroupLocation(_partyMoveToInfo.DistantGroupLoc))
                    return true;
            }

            if (!Targeting.Instance.IsEmpty())
            {
                TreeRoot.StatusText = null;
                return false;
            }

            if (await CreateInteractPoiBehavior())
                return true;
            if (await MoveToHotspot())
                return true;
            if (await ExecuteTankMovementToBoss())
                return true;

            TreeRoot.StatusText = null;
            return false;
        }

        private async Task<bool> CreateInteractPoiBehavior()
        {
            if (BotPoi.Current.Type != PoiType.Interact)
                return false;

            var myLocation = Me.Location;
            var poiObject = BotPoi.Current.AsObject;

            if (poiObject == null && myLocation.DistanceSqr(BotPoi.Current.Location) < 10 * 10)
            {
                BotPoi.Clear(string.Format("{0} was not found at {1}", BotPoi.Current.Name, BotPoi.Current.Location));
                return false;
            }

            if (poiObject == null || !poiObject.WithinInteractRange)
            {
                var destination = poiObject != null ? poiObject.Location : BotPoi.Current.Location;
                var moveResult = await CommonCoroutines.MoveTo(destination);
                if (!moveResult.IsSuccessful())
                {
                    BotPoi.Clear(string.Format("Cannot navigate to {0}", BotPoi.Current.Name));
                    Blacklist.Add(poiObject, BlacklistFlags.Interact, TimeSpan.FromMinutes(3), "Cannot navigate to object.");
                    return false;
                }

                TreeRoot.StatusText = string.Format("Moving towards object {0}", BotPoi.Current.Name);
                return true;
            }

            if (Me.Mounted)
                await CommonCoroutines.Dismount("Interacting with object");
            await ScriptHelpers.StopMovingIfMoving();

            var watcher = new CastInterruptWatcher();
            try
            {
                poiObject.Interact();
                await CommonCoroutines.SleepForLagDuration();
                await Coroutine.Sleep(500);
                await Coroutine.Wait(15000, () => !Me.IsCasting || watcher.IsInterrupted);

                if (!watcher.IsInterrupted)
                {
                    Blacklist.Add(poiObject, BlacklistFlags.Interact, TimeSpan.FromMinutes(3), "Interacted");
                    BotPoi.Clear("Interacted with object");
                }
            }
            finally
            {
                watcher.Dispose();
            }

            return true;
        }

        private async Task<bool> MoveToHotspot()
        {
            if (BotPoi.Current.Type != PoiType.Hotspot || BotPoi.Current.Location == WoWPoint.Zero)
                return false;

            if (Me.Location.DistanceSqr(BotPoi.Current.Location) < 4 * 4)
            {
                BotPoi.Clear("Reached Hotspot location");
                return false;
            }

            var moveResult = await CommonCoroutines.MoveTo(BotPoi.Current.Location);
            if (!moveResult.IsSuccessful())
            {
                BotPoi.Clear(string.Format("Cannot navigate to {0}", BotPoi.Current.Location));
                return false;
            }

            TreeRoot.StatusText = string.Format("Moving towards hotspot {0}", BotPoi.Current.Location);
            return true;
        }

        private async Task<bool> MoveToDistantGroupLocation(WoWPoint location)
        {
            if (location == WoWPoint.Zero)
                return false;

            if (!Navigator.AtLocation(location))
            {
                TreeRoot.StatusText = "Moving towards group location.";
                await CommonCoroutines.MoveTo(location, "group location");
            }

            return true;
        }

        private async Task<bool> MoveToDeadPartyMember(GroupMember[] deadMembers)
        {
            if (!deadMembers.Any())
                return false;

            var member = deadMembers[0];
            if (!Navigator.AtLocation(member.Location))
            {
                TreeRoot.StatusText = string.Format("Moving towards dead party member {0}", member.UnitId);
                await CommonCoroutines.MoveTo(member.Location);
            }

            return true;
        }

        private Task<bool> ExecuteTankMovementToBoss()
        {
            return (_tankMovementExecutor ??
                    (_tankMovementExecutor =
                         new HookExecutor(
                             "Dungeonbot_TankMovement",
                             "Dungeonbuddy's behavior for moving tank while out of combat",
                             new ActionRunCoroutine(ctx => MoveToBoss())))).ExecuteCoroutine();
        }

        private async Task<bool> MoveToBoss()
        {
            WoWPoint destination;
            string targetName;

            var bossUnit = GetBoss();
            if (bossUnit != null)
            {
                destination = bossUnit.Location;
                targetName = bossUnit.SafeName;
            }
            else
            {
                var currentBoss = BossManager.CurrentBoss;
                if (currentBoss == null)
                    return false;

                destination = currentBoss.PathBreadCrumbs.Peek();
                if (Me.Location.DistanceSqr(destination) < 5 * 5)
                    currentBoss.PathBreadCrumbs.Dequeue();
                targetName = currentBoss.Name;
            }

            TreeRoot.StatusText = string.Format("Moving towards boss {0}", targetName);
            await CommonCoroutines.MoveTo(destination);
            return true;
        }

        private Composite CreateTankWaitBehavior()
        {
            return new PrioritySelector(
                new Decorator(
                    ctx => Targeting.Instance.FirstUnit.IsBoss() && !IsFarming,
                    new PrioritySelector(
                        CreateWaitForPartyBehavior(
                            ctx => !_partyMoveToInfo.DeadGroupMembers.Any() && Me.PartyMembers.Any(p => p.HealthPercent < 75 && !p.Combat),
                            "Waiting on party members to heal up before pulling boss"),
                        CreateWaitForPartyBehavior(
                            ctx =>
                            !_partyMoveToInfo.DeadGroupMembers.Any() && Me.PartyMembers.Any(p => p.PowerType == WoWPowerType.Mana && p.ManaPercent < 75 && !p.Combat) &&
                            ManaRegenTimerIsRunning,
                            "Waiting on party members to gain more mana before pulling boss"),
                        CreateWaitForPartyBehavior(
                            ctx =>
                            !_partyMoveToInfo.DeadGroupMembers.Any() &&
                            Me.RaidMembers.Count(p => p != null && p.DistanceSqr < 30 * 30) != GroupMember.GroupMembers.Count,
                            "Waiting on party members to move closer"))),
                CreateWaitForPartyBehavior(ctx => _partyMoveToInfo.WaitForHealer, "Waiting for Healer to get in range"),
                CreateWaitForPartyBehavior(ctx => _partyMoveToInfo.WaitForDps, "Waiting for more DPS to get in range"),
                CreateWaitForPartyBehavior(
                    ctx =>
                    GroupMember.GroupMembers.Any(m => m.IsHealer && m.MapId == Me.MapId && m.IsAlive && m.PowerPercent <= 25 && m.Player != null && !m.Player.Combat) &&
                    ManaRegenTimerIsRunning,
                    "Waiting on healer to gain more mana"),
                CreateWaitForPartyBehavior(
                    ctx => Me.PartyMembers.Any(p => (p.HasAura("Drink") && p.ManaPercent < 100) || (p.HasAura("Food") && p.HealthPercent < 100)),
                    "Waiting on party member to finish eating"));
        }

        private static bool IsPointNearPathToBoss(WoWPoint point, float distanceToPath)
        {
            if (BossManager.CurrentBoss == null)
                return false;

            if (_pathResult == null || _pathToBossTimer.IsFinished)
            {
                _pathResult = DungeonNavigator.Nav.FindPath(StyxWoW.Me.Location, BossManager.CurrentBoss.Location);
                _pathToBossTimer.Reset();
            }

            if (!_pathResult.Succeeded)
                return false;

            distanceToPath *= distanceToPath;

            for (int i = 1; i < _pathResult.Points.Length; i++)
            {
                var segmentStart = _pathResult.Points[i - 1];
                var segmentEnd = _pathResult.Points[i];
                if (point.GetNearestPointOnSegment(segmentStart, segmentEnd).DistanceSqr(point) <= distanceToPath)
                    return true;
            }
            return false;
        }

        private Decorator CreateWaitForPartyBehavior(CanRunDecoratorDelegate canRun, string reasonFormat, params object[] reasonArgs)
        {
            return new Decorator(
                canRun,
                new Sequence(
                    new Action(ctx => TreeRoot.StatusText = String.Format(reasonFormat, reasonArgs)),
                    new Action(ctx => Navigator.NavigationProvider.StuckHandler.Reset()),
                    new DecoratorContinue(
                        ctx => Me.IsMoving, new Sequence(new Action(ctx => WoWMovement.MoveStop()), new WaitContinue(2, ret => !Me.IsMoving, new ActionAlwaysSucceed())))));
        }

        private WoWUnit GetBoss()
        {
            return
                ObjectManager.GetObjectsOfType<WoWUnit>()
                             .Where(u => u.IsAlive && u.IsBoss() && ShouldKillBoss(u) && DungeonNavigator.CanNavigateDistanceFully(Me.Location, u.Location, 100f))
                             .OrderBy(u => u.ToBoss().KillOrder)
                             .ThenBy(u => u.DistanceSqr)
                             .FirstOrDefault();
        }

        private bool ShouldKillBoss(WoWUnit unit)
        {
            var currentBoss = BossManager.CurrentBoss;
            Boss boss = unit.ToBoss();
            if (currentBoss != null && unit.Entry == currentBoss.Entry)
                return true;
            if (boss != null && boss.IsRandomSpawnBoss() && boss.ShouldKillBoss() && ((currentBoss != null && boss.KillOrder <= currentBoss.KillOrder) || currentBoss == null))
                return true;
            return false;
        }

        #region Nested type: CastInterruptWatcher

        private class CastInterruptWatcher : IDisposable
        {
            public bool IsInterrupted { get; private set; }

            public CastInterruptWatcher()
            {
                Lua.Events.AttachEvent("UNIT_SPELLCAST_FAILED", HandleSpellcastInterrupted);
                Lua.Events.AttachEvent("UNIT_SPELLCAST_INTERRUPTED", HandleSpellcastInterrupted);
            }

            private void HandleSpellcastInterrupted(object sender, LuaEventArgs args)
            {
                var unitId = args.Args[0].ToString();
                if (unitId != "player")
                    return;

                var spellName = args.Args[1].ToString();
                var eventArg = args.Args[4].ToString();
                Logger.Write("\"{0}\"({1}) interrupted via {2} Event.", spellName, eventArg, args.EventName);
                IsInterrupted = true;
            }

            public void Dispose()
            {
                Lua.Events.DetachEvent("UNIT_SPELLCAST_FAILED", HandleSpellcastInterrupted);
                Lua.Events.DetachEvent("UNIT_SPELLCAST_INTERRUPTED", HandleSpellcastInterrupted);
            }
        }

        #endregion

        #region Nested type: GroupInfoMisc

        internal class GroupInfoMisc
        {
            private static readonly Stopwatch MoveToGroupSw = new Stopwatch();
            private static ulong _groupMemberMoveToGuid;
            private static DateTime _prevCorpseRunTime;

            public GroupMember[] DeadGroupMembers { get; private set; }
            public WoWPoint DistantGroupLoc { get; private set; }
            public bool WaitForHealer { get; private set; }
            public bool WaitForDps { get; private set; }

            public static GroupInfoMisc GetPartyMovetoInfo()
            {
                var myLoc = Me.Location;
                var partyMoveTo = new GroupInfoMisc
                                  {
                                      DeadGroupMembers =
                                          GroupMember.GroupMembers.Where(
                                              m => m.MapId == Me.MapId && m.IsOnline && m.IsDead && Navigator.CanNavigateFully(myLoc, m.Location))
                                                     .OrderBy(m => m.Location.DistanceSqr(myLoc))
                                                     .ToArray()
                                  };
                var moveToGroup = false;
                if (MoveToGroupSw.IsRunning && MoveToGroupSw.Elapsed >= TimeSpan.FromSeconds(120) || _prevCorpseRunTime != _lastCorpseRunTime)
                {
                    var moveTo = WoWPoint.Zero;

                    GroupMember distantPlayer = null;
                    if (_groupMemberMoveToGuid != 0)
                        distantPlayer = GroupMember.GroupMembers.FirstOrDefault(g => g.Guid == _groupMemberMoveToGuid && Navigator.CanNavigateFully(myLoc, g.Location));
                    if (distantPlayer == null)
                        distantPlayer =
                            GroupMember.GroupMembers.OrderByDescending(g => g.Location.DistanceSqr(myLoc))
                                       .FirstOrDefault(g => Navigator.CanNavigateFully(myLoc, g.Location));
                    if (distantPlayer != null)
                    {
                        moveTo = distantPlayer.Location;
                        _groupMemberMoveToGuid = distantPlayer.Guid;
                    }
                    if (moveTo != WoWPoint.Zero)
                    {
                        if (myLoc.Distance(moveTo) > Navigator.PathPrecision)
                        {
                            partyMoveTo.DistantGroupLoc = moveTo;
                            moveToGroup = true;
                        }
                        else
                        {
                            _groupMemberMoveToGuid = 0;
                            _prevCorpseRunTime = _lastCorpseRunTime;
                            MoveToGroupSw.Reset();
                        }
                    }
                }

                partyMoveTo.WaitForHealer = !moveToGroup && ShouldWaitForHealer();

                partyMoveTo.WaitForDps = !moveToGroup && (from member in GroupMember.GroupMembers
                                                          where member.IsDps
                                                          let inDungeon = member.MapId == Me.MapId
                                                          let loc = member.Location
                                                          where inDungeon && loc.DistanceSqr(myLoc) <= MaxFollowRangeSqr || IsPointNearPathToBoss(loc, 40)
                                                          select member).Count() < Math.Ceiling((double)GroupMember.GroupMembers.Count(g => g.IsDps) / 2);

                if (partyMoveTo.WaitForHealer || partyMoveTo.WaitForDps)
                {
                    if (!MoveToGroupSw.IsRunning)
                        MoveToGroupSw.Start();
                }
                else if (MoveToGroupSw.IsRunning && !moveToGroup)
                    MoveToGroupSw.Reset();

                return partyMoveTo;
            }

            private static bool ShouldWaitForHealer()
            {
                if (DungeonManager.SelectedDungeon == null)
                    return false;

                if (DungeonManager.SelectedDungeon.LfgDungeon.SubType == LfgDungeons.LfgType.Scenario)
                    return false;

                var myMapId = Me.MapId;
                var myLoc = Me.Location;
                return GroupMember.GroupMembers.Any(
                    member =>
                    {
                        var inDungeon = member.MapId == myMapId;
                        var loc = member.Location;
                        return member.IsHealer &&
                               ((inDungeon && member.IsAlive && loc.DistanceSqr(myLoc) > MaxFollowRangeSqr && !IsPointNearPathToBoss(loc, 40)) || !inDungeon);
                    });
            }
        }

        #endregion
    }
}
