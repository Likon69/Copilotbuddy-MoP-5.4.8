using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bots.DungeonBuddy.Avoidance;
using Buddy.Coroutines;
using Styx.CommonBot.Coroutines;
using CommonBehaviors.Actions;
using CommonBehaviors.Decorators;
using Styx;
using Styx.CommonBot.Routines;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.Routines;
using Styx.Helpers;
using Styx.CommonBot.POI;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWCache;
using Styx.WoWInternals.WoWObjects;
using Styx.TreeSharp;
using Tripper.Navigation;
using Action = Styx.TreeSharp.Action;
using AvoidanceManager = Bots.DungeonBuddy.Avoidance.AvoidanceManager;
using Bots.DungeonBuddy.Behaviors;
using Bots.DungeonBuddy.Helpers;

namespace Bots.DungeonBuddy
{
    public partial class DungeonBot
    {
        private static readonly WaitTimer TargetTimer = new WaitTimer(TimeSpan.FromSeconds(2));

        private static readonly WaitTimer TimeSinceCombatEndTimer = new WaitTimer(TimeSpan.FromSeconds(3));

        private readonly WaitTimer _tankRangedPullTimer = new WaitTimer(TimeSpan.FromSeconds(5));
        private readonly WaitTimer _moveBehindTimer = new WaitTimer(TimeSpan.FromSeconds(3));
        private static readonly WaitTimer _pullTimeoutTimer = new WaitTimer(TimeSpan.FromSeconds(30));
        private static ulong _pullTargetGuid;
        private readonly Tripper.Tools.Math.Vector3 _meshTraceExtents = new Tripper.Tools.Math.Vector3(3f, 5f, 3f);
        private static readonly HashSet<uint> NoMoveBehindEntries = new HashSet<uint>
        {
            5925u,
            33993u,
            107194u,
            24043u,
            36627u,
            36626u,
            49045u,
            44854u
        };

        private Composite? _combatMovementBehavior;

        private static CombatRoutine Routine
        {
            get { return (CombatRoutine)RoutineManager.Current; }
        }

        private bool ShouldWipe
        {
            get
            {
                if (GroupMember.GroupMembers.Count <= 1)
                    return false;
                // call wipe if in raid and 75% or more members are dead.
                if (Me.CurrentMap.IsRaid)
                {
                    var numberOfRaidMembers = GroupMember.GroupMembers.Count;
                    return GroupMember.GroupMembers.Count(g => g.Guid != Me.Guid && !g.IsAlive || g.Guid == Me.Guid) >= numberOfRaidMembers * 0.75f;
                }
                // in 5 mans call wipe if everyone but self is dead.
                if (Me.CurrentMap.IsDungeon)
                    return GroupMember.GroupMembers.All(g => g.Guid != Me.Guid && !g.IsAlive || g.Guid == Me.Guid);
                return false;
            }
        }

        private Composite CreateCombatBehavior()
        {
            return new CombatRoutineDecorator(
                new PrioritySelector(
                    // we need to set the RAFLeader because some CRs will use that.
                    new PrioritySelector(
                        ctx => GroupMember.GroupMembers.Where(p => p.IsLeader() && p.Player != null).OrderByDescending(p => p.MaxHealth).Select(p => p.Player).FirstOrDefault(),
                        new Decorator<WoWPlayer>(
                            player => Me.GroupInfo.IsInParty && RaFHelper.Leader != player, new Helpers.Action<WoWPlayer>(player => RaFHelper.SetLeader(player)))),
                    //Out of combat stuff
                    new Decorator(
                        ret => !Me.Combat,
                        new PrioritySelector(
                            //Rest Behavior first
                            Routine.RestBehavior,
                            new Decorator(
                                ret => !TimeSinceCombatEndTimer.IsFinished && Me.IsTank() && Me.GroupInfo.IsInParty &&
                                       (DungeonManager.CurrentDungeon == null || Me.Level - DungeonManager.CurrentDungeon.LfgDungeon.RecommendedLevel < 5),
                                new PrioritySelector(
                                    ctx => Targeting.Instance.FirstUnit,
                                    new Decorator(
                                        ctx => ctx == null || !((WoWUnit)ctx).IsTargetingMyRaidMember,
                                        new PrioritySelector(
                                            new Decorator(ctx => Me.IsMoving, new Action(ctx => WoWMovement.MoveStop())),
                                            new ActionAlwaysSucceed())))),
                            Routine.PreCombatBuffBehavior,
                            // We are gonna pull only if the poi is set to kill by tank/follow behavior
                            new DecoratorIsPoiType(
                                PoiType.Kill,
                                new PrioritySelector(
                                    // Clear poi if targetlist is empty or if combat ended less than 3 seconds ago (prevents chain pulling)
                                    new Decorator(
                                        ret =>
                                        Targeting.Instance.TargetList.Count == 0 || Targeting.Instance.FirstUnit.IsDead ||
                                        (!TimeSinceCombatEndTimer.IsFinished && !Targeting.Instance.FirstUnit.IsTargetingMyPartyMember &&
                                         !Targeting.Instance.FirstUnit.TaggedByMe),
                                        new Sequence(
                                            new ActionClearPoi("No targets in list (pull)"),
                                            new DecoratorContinue(
                                                ret => Me.CurrentTarget != null,
                                                new Sequence(
                                                    new Action(ret => Me.ClearTarget()), new WaitContinue(5, ret => Me.CurrentTarget == null, new ActionAlwaysSucceed()))))),
                                    // Force the 'correct' POI to be our target.
                                    new Decorator(
                                        ret => BotPoi.Current.AsObject != null && BotPoi.Current.AsObject.ToUnit() != Targeting.Instance.FirstUnit,
                                        new Sequence(
                                            new Action(ret => Logger.WriteDebug("Current POI is not the best target. Changing.")),
                                            new ActionSetPoi(true, ret => new BotPoi(Targeting.Instance.FirstUnit, PoiType.Kill)))),
                                    // Make sure target is in Los before we give CC control
                                    new Decorator(
                                        ctx => !Targeting.Instance.FirstUnit.InLineOfSpellSight,
                                        new PrioritySelector(
                                            // blaclist units that we can't navigate to.
                                            new Decorator(
                                                ctx => !Navigator.CanNavigateFully(Me.Location, Targeting.Instance.FirstUnit.Location, 20),
                                                new Action(ctx => Blacklist.Add(Targeting.Instance.FirstUnit, TimeSpan.FromMinutes(25)))),
                                            new NavigationAction(ctx => Targeting.Instance.FirstUnit.Location))),
                                    // Make sure we have the proper unit as target
                                    new Decorator(
                                        ret => (TargetTimer.IsFinished && Me.CurrentTarget != BotPoi.Current.AsObject.ToUnit()) || Me.CurrentTargetGuid == 0,
                                        new Sequence(
                                            // pending spells like mage blizard cause targeting to fail.
                                            new DecoratorContinue(ctx => StyxWoW.Me.CurrentPendingCursorSpell != null, new Action(ctx => Lua.DoString("SpellStopTargeting()"))),
                                            new Action(
                                                ret =>
                                                    {
                                                        var unit = BotPoi.Current.AsObject.ToUnit();
                                                        unit.Target();
                                                        _pullTargetGuid = unit.Guid;
                                                        _pullTimeoutTimer.Reset();
                                                        TargetTimer.Reset();
                                                        return Me.IsHealer() ? RunStatus.Failure : RunStatus.Success;
                                                    }),
                                            new WaitContinue(
                                                5, ret => Me.CurrentTargetGuid != 0 && Me.CurrentTarget == BotPoi.Current.AsObject.ToUnit(), new ActionAlwaysSucceed()))),
                                    new Decorator(
                                        ret => Me.CurrentTargetGuid != _pullTargetGuid,
                                        new Action(
                                            ret =>
                                                {
                                                    _pullTargetGuid = Me.CurrentTargetGuid;
                                                    if (_pullTargetGuid != 0)
                                                        _pullTimeoutTimer.Reset();
                                                    return RunStatus.Failure;
                                                })),
                                    new Decorator(
                                        ret => _pullTimeoutTimer.IsFinished && Me.CurrentTargetGuid == _pullTargetGuid && _pullTargetGuid != 0,
                                        new Sequence(
                                            ctx => Me.CurrentTarget,
                                            new Action(ret => Blacklist.Add(_pullTargetGuid, BlacklistFlags.Pull, TimeSpan.FromMinutes(3), "Took too long to pull")),
                                            new Action(ret => _pullTargetGuid = 0),
                                            new Action(ret => Me.ClearTarget()))),
                                    Routine.PullBuffBehavior,
                                    CreateCombatMovementBehavior(),
                                    // Move to target if custom class supports it
                                    new Decorator(ret => Routine.MoveToTargetBehavior != null, Routine.MoveToTargetBehavior),
                                    Routine.PullBehavior)))),
                    //Combat stuff
                    new Decorator(
                        ret => Me.Combat,
                        new PrioritySelector(
                            // reset TimeSinceCombatEndTimer whenever in combat - return failure to cause behavior to keep going.
                            new Action(
                                ctx =>
                                    {
                                        TimeSinceCombatEndTimer.Reset();
                                        return RunStatus.Failure;
                                    }),
                            // If healer and almost everyone else is dead than stop healing..
                            new Decorator(
                                ctx => Me.IsHealer() && ShouldWipe, new PrioritySelector(new ActionSetActivity("Stoping heals to not prolong wipe"), new ActionAlwaysSucceed())),
                            new DecoratorIsPoiType(
                                PoiType.Kill,
                                new PrioritySelector(
                                    // Clear poi if targetlist is empty
                                    new Decorator(
                                        ret =>
                                        Targeting.Instance.FirstUnit == null || Targeting.Instance.FirstUnit.IsDead || BotPoi.Current.AsObject == null ||
                                        !BotPoi.Current.AsObject.IsValid,
                                        new Sequence(
                                            new ActionClearPoi("No targets in list (combat)"),
                                            new DecoratorContinue(
                                                ret => Me.CurrentTarget != null,
                                                new Sequence(
                                                    new Action(ret => Me.ClearTarget()), new WaitContinue(2, ret => Me.CurrentTarget == null, new ActionAlwaysSucceed()))))),
                                    // Force the 'correct' POI to be our target.
                                    new Decorator(
                                        ret => BotPoi.Current.AsObject.ToUnit() != Targeting.Instance.FirstUnit,
                                        new Sequence(
                                            new Action(
                                                ctx =>
                                                    { // this needs to keep tranversing down the behavior as we can not consume ticks with POI changes on big pulls.
                                                        Logger.WriteDebug("Current POI is not the best pull target. Changing.");
                                                        BotPoi.Current = new BotPoi(Targeting.Instance.FirstUnit, PoiType.Kill);
                                                        return RunStatus.Failure;
                                                    }))),
                                    // Make sure we have the proper unit as target
                                    new Decorator(
                                        ret => (TargetTimer.IsFinished && Me.CurrentTarget != BotPoi.Current.AsObject.ToUnit()) || Me.CurrentTarget == null,
                                        new Sequence(
                                            // pending spells like mage blizard cause targeting to fail.
                                            new DecoratorContinue(ctx => StyxWoW.Me.CurrentPendingCursorSpell != null, new Action(ctx => Lua.DoString("SpellStopTargeting()"))),
                                            new Action(
                                                ret =>
                                                    {
                                                        BotPoi.Current.AsObject.ToUnit().Target();
                                                        TargetTimer.Reset();
                                                    }),
                                            new WaitContinue(
                                                2,
                                                ret => Me.CurrentTarget != null && BotPoi.Current.AsObject != null && Me.CurrentTarget == BotPoi.Current.AsObject.ToUnit(),
                                                new ActionAlwaysSucceed()))),
                                    new Decorator(
                                        ret => !DungeonBuddySettings.Instance.CombatMovement && Me.Mounted,
                                        new ActionRunCoroutine(ret => CommonCoroutines.Dismount("Combat"))),
                                    Routine.HealBehavior,
                                    Routine.CombatBuffBehavior,
                                    CreateCombatMovementBehavior(),
                                    Routine.CombatBehavior))))));
        }

        private Composite CreateCombatMovementBehavior()
        {
            if (_combatMovementBehavior == null)
            {
                _combatMovementBehavior = new ActionRunCoroutine(context => HandleCombatMovement());
            }

            return _combatMovementBehavior;
        }

        private bool ShouldHandleCombatMovement
        {
            get
            {
                if (DungeonBuddySettings.Instance.CombatMovement)
                {
                    return true;
                }

                if (Routine.SupportedCapabilities != CapabilityFlags.None && !Routine.HasCapability(CapabilityFlags.Movement))
                {
                    return RoutineManager.GetCapabilityState(CapabilityFlags.Movement) != CapabilityState.Disallowed;
                }

                return false;
            }
        }

        private async Task<bool> HandleCombatMovement()
        {
            if (AvoidanceManager.IsRunningOutOfAvoid || !ShouldHandleCombatMovement)
            {
                return false;
            }

            if (Me.Specialization.IsTank() && await HandleTankCombatMovement())
            {
                return true;
            }

            if (Me.IsHealer() && await HandleHealerCombatMovement())
            {
                return true;
            }

            if (Me.IsMeleeDps() && await HandleMeleeCombatMovement())
            {
                return true;
            }

            if (Me.IsRangeDps() && await HandleRangedCombatMovement())
            {
                return true;
            }

            return false;
        }

        private async Task<bool> HandleTankCombatMovement()
        {
            WoWUnit? target = Me.CurrentTarget;
            if (target == null || !target.Attackable)
            {
                return false;
            }

            WoWPoint location = target.Location;
            if (location.Distance(Me.Location) - target.MeleeRange() > 15f)
            {
                string gapCloser = GetTankGapCloser(location);
                if (!string.IsNullOrEmpty(gapCloser) && await CastGapCloser(gapCloser))
                {
                    await Coroutine.Wait(1500, () => target.IsWithinMeleeRange);
                }
                else if (!target.Combat && target.CurrentTargetGuid != Me.Guid && _tankRangedPullTimer.IsFinished)
                {
                    if (await ScriptHelpers.CastRangedAbility(target))
                    {
                        _tankRangedPullTimer.Reset();
                    }
                }
            }

            if (target.Distance > target.MeleeRange())
            {
                Navigator.MoveTo(location);
                return true;
            }

            if (Me.IsMoving && target.Distance < target.MeleeRange() - 2f)
            {
                WoWMovement.MoveStop();
                await Coroutine.Wait(2000, () => !Me.IsMoving);
            }

            if (!Me.IsSafelyFacing(target))
            {
                target.Face();
                await Coroutine.Wait(2000, () => !Me.IsSafelyFacing(target));
            }

            if (Me.Mounted)
            {
                return await CommonCoroutines.Dismount("Combat");
            }

            return false;
        }

        private string GetTankGapCloser(WoWPoint location)
        {
            switch (Me.Class)
            {
                case WoWClass.Monk:
                    if (SpellManager.CanCast("Clash"))
                    {
                        return "Clash";
                    }
                    if (WoWMathHelper.IsFacing(Me.Location, Me.Rotation, location, WoWMathHelper.DegreesToRadians(20f)))
                    {
                        return "Roll";
                    }
                    return string.Empty;
                case WoWClass.Druid:
                    return Me.Shapeshift == ShapeshiftForm.DireBear ? "Wild Charge" : string.Empty;
                case WoWClass.DeathKnight:
                    return "Death Grip";
                case WoWClass.Warrior:
                    return "Charge";
                default:
                    return string.Empty;
            }
        }

        private async Task<bool> HandleHealerCombatMovement()
        {
            WoWPoint myLocation = Me.Location;
            GroupMember? leader = GroupMember.GroupMembers
                .Where(member => member.IsLeader())
                .OrderBy(member => member.Location.DistanceSqr(myLocation))
                .FirstOrDefault();
            WoWUnit? healTarget = HealTargeting.Instance.TargetList
                .Select(target => new { Target = target, HealthPercent = target.GetPredictedHealthPercent(true) })
                .Where(target => target.HealthPercent < 60f &&
                    (target.Target.IsMe || leader == null || target.Target.Location.DistanceSqr(leader.Location) < 4900f))
                .OrderBy(target => target.HealthPercent)
                .Select(target => target.Target)
                .FirstOrDefault() ?? (leader != null ? leader.Player : null);

            if (healTarget != null && (healTarget.Distance > GetCombatRange(healTarget) || !healTarget.InLineOfSpellSight))
            {
                await CommonCoroutines.MoveTo(healTarget.Location);
                return true;
            }

            if (healTarget != null && Me.IsMoving && healTarget.Distance < GetCombatRange(healTarget) - 15f && healTarget.InLineOfSpellSight)
            {
                WoWMovement.MoveStop();
                await Coroutine.Wait(2000, () => !Me.IsMoving);
            }

            WoWUnit? target = Me.CurrentTarget;
            if (!Me.IsCasting && target != null && !Me.IsSafelyFacing(target) && !Me.IsMoving)
            {
                target.Face();
                await Coroutine.Wait(2000, () => Me.IsSafelyFacing(target));
            }

            if (Me.Mounted)
            {
                return await CommonCoroutines.Dismount("Combat");
            }

            return false;
        }

        private async Task<bool> HandleRangedCombatMovement()
        {
            WoWUnit? target = Me.CurrentTarget;
            if (target == null)
            {
                return false;
            }

            float range = GetCombatRange(target);
            if (target.Distance > range || !target.InLineOfSpellSight)
            {
                Navigator.MoveTo(target.Location);
                return true;
            }

            if (Me.IsMoving && target.Distance < range - 5f)
            {
                WoWMovement.MoveStop();
                await Coroutine.Wait(2000, () => !Me.IsMoving);
            }

            if (!Me.IsSafelyFacing(target) && !Me.IsMoving)
            {
                target.Face();
                await Coroutine.Wait(2000, () => Me.IsSafelyFacing(target));
            }

            if (Me.Mounted)
            {
                return await CommonCoroutines.Dismount("Combat");
            }

            return false;
        }

        private async Task<bool> HandleMeleeCombatMovement()
        {
            WoWUnit? target = Me.CurrentTarget;
            if (target == null || !target.Attackable)
            {
                return false;
            }

            WoWPoint behindLocation = GetMoveBehindLocation(target);
            float distance = Me.Location.Distance(target.Location);
            float meleeRange = target.MeleeRange();
            bool isInRange = distance <= meleeRange;
            if (!isInRange)
            {
                _moveBehindTimer.Reset();
            }

            if (behindLocation != WoWPoint.Zero && _moveBehindTimer.IsFinished && !NoMoveBehindEntries.Contains(target.Entry))
            {
                if (Navigator.AtLocation(behindLocation))
                {
                    _moveBehindTimer.Reset();
                }
                else if (!target.IsMoving && !Me.IsSafelyBehind(target) && target.CurrentTargetGuid != Me.Guid)
                {
                    Navigator.MoveTo(behindLocation);
                    return false;
                }
            }

            if (!isInRange)
            {
                return await MoveToMeleeRange(target);
            }

            if (Me.IsMoving && distance < meleeRange - 2f)
            {
                WoWMovement.MoveStop();
                await Coroutine.Wait(1000, () => Me.IsMoving);
            }

            if (!Me.IsMoving && !Me.IsSafelyFacing(target))
            {
                target.Face();
                return true;
            }

            if (Me.Mounted)
            {
                return await CommonCoroutines.Dismount("Combat");
            }

            return false;
        }

        private WoWPoint GetMoveBehindLocation(WoWUnit target)
        {
            if (target.CurrentTargetGuid == Me.Guid)
            {
                return WoWPoint.Zero;
            }

            WoWPoint myLocation = Me.Location;
            return GetMoveBehindLocations(target)
                .OrderBy(location => location.DistanceSqr(myLocation))
                .FirstOrDefault(location => IsMeshLineClear(myLocation, location) &&
                    !AvoidanceManager.Avoids.Any(avoid => avoid.IsPointInAvoid(location)));
        }

        private IEnumerable<WoWPoint> GetMoveBehindLocations(WoWUnit target)
        {
            float rotation = WoWMathHelper.NormalizeRadian(target.Rotation);
            WoWPoint targetLocation = target.Location;
            WoWPoint myLocation = Me.Location;
            float meleeRange = target.MeleeRange();
            float angle = WoWMathHelper.NormalizeRadian((float)Math.PI * 3f / 4f + rotation);
            for (int index = 0; index < 3; index++)
            {
                WoWPoint innerPoint = targetLocation.RayCast(angle, 3f);
                yield return innerPoint;
                if (!(meleeRange - Navigator.PathPrecision - 3f < 2f))
                {
                    WoWPoint outerPoint = targetLocation.RayCast(angle, meleeRange - Navigator.PathPrecision);
                    yield return outerPoint;
                    WoWPoint nearestPoint = myLocation.GetNearestPointOnSegment(innerPoint, outerPoint);
                    if (nearestPoint != outerPoint && nearestPoint != innerPoint)
                    {
                        yield return nearestPoint;
                    }
                    angle = WoWMathHelper.NormalizeRadian(angle + (float)Math.PI / 4f);
                }
            }
        }

        private async Task<bool> MoveToMeleeRange(WoWUnit target)
        {
            float meleeRange = target.MeleeRange();
            WoWPoint location = target.Location;
            if (Me.Location.Distance(location) - meleeRange > 15f)
            {
                string gapCloser = GetMeleeGapCloser(location);
                if (!string.IsNullOrEmpty(gapCloser) && await CastGapCloser(gapCloser))
                {
                    return true;
                }
            }

            Navigator.MoveTo(location);
            return false;
        }

        private string GetMeleeGapCloser(WoWPoint location)
        {
            switch (Me.Class)
            {
                case WoWClass.Monk:
                    return WoWMathHelper.IsFacing(Me.Location, Me.Rotation, location, WoWMathHelper.DegreesToRadians(20f)) ? "Roll" : string.Empty;
                case WoWClass.Druid:
                    return "Dash";
                case WoWClass.Warrior:
                    return "Charge";
                case WoWClass.Paladin:
                    return "Judgment";
                case WoWClass.Rogue:
                    return "Sprint";
                default:
                    return string.Empty;
            }
        }

        private float GetCombatRange(WoWUnit target)
        {
            if (!Me.IsMelee())
            {
                return (float)((Me.IsCasting || Me.IsChanneling) ? 40 : 35) + target.CombatReach + 1f;
            }

            return target.MeleeRange();
        }

        private bool IsMeshLineClear(WoWPoint start, WoWPoint end)
        {
            WowNavigator navigator = ((MeshNavigator)Navigator.NavigationProvider).Nav;
            Tripper.Tools.Math.Vector3 nearestPoint = Tripper.Navigation.NavHelper.ToNav(start);
            Tripper.Tools.Math.Vector3 endPoint = Tripper.Navigation.NavHelper.ToNav(end);
            Tripper.RecastManaged.Detour.PolygonReference polygon;
            if (!navigator.MeshQuery.FindNearestPolygon(nearestPoint, _meshTraceExtents, Tripper.RecastManaged.Detour.QueryFilter.Default, out nearestPoint, out polygon).Succeeded)
            {
                return false;
            }

            Tripper.RecastManaged.Detour.PolygonReference[] polygons;
            float distance;
            Tripper.Tools.Math.Vector3 normal;
            return navigator.MeshQuery.Raycast(polygon, nearestPoint, endPoint, Tripper.RecastManaged.Detour.QueryFilter.Default, 30, out polygons, out distance, out normal).Succeeded && distance == float.MaxValue;
        }

        private async Task<bool> CastGapCloser(string spellName)
        {
            WoWUnit? target = Me.CurrentTarget;
            if (target == null || !SpellManager.CanCast(spellName, true))
            {
                return false;
            }

            if (!Me.IsSafelyFacing(target))
            {
                target.Face();
                await Coroutine.Wait(1000, () => Me.IsSafelyFacing(target));
            }

            return SpellManager.Cast(spellName);
        }
    }
}
