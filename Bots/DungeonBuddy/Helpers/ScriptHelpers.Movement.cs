using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Buddy.Coroutines;
using CommonBehaviors.Actions;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot.Routines;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.Helpers;
using Styx.CommonBot.POI;
using Styx.Patchables;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Styx.TreeSharp;
using Tripper.Tools.Math;
using Action = Styx.TreeSharp.Action;
#if USE_DUNGEONBUDDY_DLL
using AvoidanceManager = Bots.DungeonBuddy.Avoidance.AvoidanceManager;
using Bots.DungeonBuddy.Avoidance;

namespace Bots.DungeonBuddy.Helpers
#else
    using AvoidanceManager = Bots.DungeonBuddy.Avoidance.AvoidanceManager;
    using Bots.DungeonBuddy.Avoidance;
    namespace Bots.DungeonBuddy.Helpers
#endif
{
    public static partial class ScriptHelpers
    {
        private const float PIx2 = (float)Math.PI * 2;
        private static readonly List<ControlledMoveToCondition> DisableMovementConditions = new List<ControlledMoveToCondition>();

        private static WoWPoint _controlledMoveToPoint;
        private static IStuckHandler _originalStuckHandler;

        #region Embedded Type - NoUnstuck

        public class NoUnstuck : StuckHandler
        {
            #region IStuckHandler Members

            public override bool IsStuck()
            {
                return false;
            }

            public override void Reset() { }
            public override void Unstick() { }

            #endregion
        }

        #endregion

        /// <summary>
        ///     Returns true if 'ControlledMovement' is active.
        /// </summary>
        /// <value>
        ///     <c>true</c> if [movement enabled]; otherwise, <c>false</c> .
        /// </value>
        public static bool MovementEnabled
        {
            get { return !(Navigator.PlayerMover is NoMover); }
        }

        /// <summary>
        ///     Spreads the ranged dps out, Healer doesn't move unless too close to melee.
        /// </summary>
        /// <param name="canRun"> The can run. </param>
        /// <param name="distanceToSpread"> The distance to spread. </param>
        /// <returns> </returns>
        public static Composite CreateSpreadOutLogic(CanRunDecoratorDelegate canRun, int distanceToSpread)
        {
            return CreateSpreadOutLogic(canRun, context => Tank.Location, distanceToSpread, 35f);
        }

        /// <summary>
        ///     Spreads the ranged dps out, Healer doesn't move unless too close to melee.
        /// </summary>
        /// <param name="canRun"> The condition under which to run this behavior. </param>
        /// <param name="centerPointSelector"> center location of the area to spread out </param>
        /// <param name="distanceToSpread"> The distance to spread out from other party members. </param>
        /// <param name="maxDistanceFromCenterPoint"> max distance to move from center point </param>
        /// <returns> </returns>
        public static Composite CreateSpreadOutLogic(
            CanRunDecoratorDelegate canRun, Func<object, WoWPoint> centerPointSelector, int distanceToSpread, float maxDistanceFromCenterPoint)
        {
            return new Decorator(canRun,
                new ActionRunCoroutine(context => SpreadOut(centerPointSelector(context), distanceToSpread, maxDistanceFromCenterPoint)));
        }

        /// <summary>
        ///     tank unit at location.
        /// </summary>
        /// <param name="locationSelector"> The location selector. </param>
        /// <param name="precision"> The precision. </param>
        /// <returns> </returns>
        public static Composite CreateTankUnitAtLocation(Func<object, WoWPoint> locationSelector, float precision)
        {
            return new ActionRunCoroutine(context => TankUnitAtLocation(locationSelector(context), precision));
        }

        /// <summary>
        ///     Jumps down from a high spot to a lower one. Pet classes will dismiss pets 1st. Tank will jump if canRun is 'true' and followers will take the jump if canRun is <c>true</c> and if tank took the jump.
        /// </summary>
        /// <param name="canRun"> The condition. </param>
        /// <param name="from"> From. </param>
        /// <param name="to"> To. </param>
        /// <returns> </returns>
        public static Composite CreateJumpDown(CanRunDecoratorDelegate canRun, WoWPoint from, WoWPoint to)
        {
            return CreateJumpDown(canRun, false, from, to);
        }

        /// <summary>
        ///     Jumps down from a high spot to a lower one. Pet classes will dismiss pets 1st. Tank will jump if canRun is 'true' and followers will take the jump if canRun is <c>true</c> and if tank took the jump.
        /// </summary>
        /// <param name="canRun"> The condition. </param>
        /// <param name="actuallyJump">
        ///     if set to <c>true</c> jumping is actually used verses just dropping down.
        /// </param>
        /// <param name="from"> From. </param>
        /// <param name="to"> To. </param>
        /// <param name="dismissPet"></param>
        /// <returns> </returns>
        public static Composite CreateJumpDown(CanRunDecoratorDelegate canRun, bool actuallyJump, WoWPoint from, WoWPoint to, bool dismissPet = true)
        {
            return
                new PrioritySelector(
                    new Decorator(
                        ctx => canRun(ctx) && StyxWoW.Me.Z > to.Z + 6,
                        new PrioritySelector(
                            new Decorator(
                                ctx =>
                                StyxWoW.Me.IsTank() || Tank != null && Tank.Z < from.Z - 6 && !DungeonBot.DungeonNavigator.CanNavigateDistanceFully(from, Tank.Location, 50f),
                                new Sequence(
                                    new DecoratorContinue(ctx => StyxWoW.Me.Location.DistanceSqr(from) > 5 * 5, CreateMoveToContinue(context => from)),
                                    new DecoratorContinue(
                                        ctx => dismissPet && StyxWoW.Me.Class == WoWClass.Hunter && StyxWoW.Me.GotAlivePet,
                                        new Sequence(
                                            new DecoratorContinue(
                                                ctx => StyxWoW.Me.IsMoving,
                                                new Sequence(
                                                    new Action(ctx => WoWMovement.MoveStop()), new WaitContinue(2, ctx => !StyxWoW.Me.IsMoving, new ActionAlwaysSucceed()))),
                                            new Action(ctx => SpellManager.Cast("Dismiss Pet")),
                                            new WaitContinue(3, ctx => false, new ActionAlwaysSucceed()))),
                                    new DecoratorContinue(
                                        ctx => dismissPet && StyxWoW.Me.Class != WoWClass.Hunter && StyxWoW.Me.GotAlivePet, new Action(ctx => Lua.DoString("PetDismiss()"))),
                                    new DecoratorContinue(
                                        ctx => StyxWoW.Me.Z > to.Z + 6,
                                        new Sequence(
                                            new Action(ctx => WoWMovement.ClickToMove(to)),
                                            new DecoratorContinue(ctx => actuallyJump, new Action(ctx => WoWMovement.Move(WoWMovement.MovementDirection.JumpAscend))),
                                            new WaitContinue(2, ctx => false, new ActionAlwaysSucceed()),
                                            new DecoratorContinue(ctx => actuallyJump, new Action(ctx => WoWMovement.MoveStop(WoWMovement.MovementDirection.JumpAscend))))))))));
        }

        /// <summary>
        ///     Jumps from one spot to another. Pet classes will dismiss pets 1st. Everyone Always Jumps if the condition is true.
        /// </summary>
        /// <param name="canRun"> The condition. </param>
        /// <param name="from"> From. </param>
        /// <param name="to"> To. </param>
        /// <returns> </returns>
        public static Composite CreateForceJump(CanRunDecoratorDelegate canRun, WoWPoint from, WoWPoint to, bool dismissPet = true)
        {
            return CreateJumpDown(canRun, false, from, to, dismissPet);
        }

        public static async Task ForceJump(WoWPoint startPos, WoWPoint jumpAtLocation, bool stopRunning = true)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (StyxWoW.Me.Location.Distance2DSqr(startPos) > 1f && stopwatch.ElapsedMilliseconds < 5000L)
            {
                Navigator.PlayerMover.MoveTowards(startPos);
                await Coroutine.Yield();
            }
            if (StyxWoW.Me.Location.Distance2DSqr(startPos) > 1f)
                return;

            await Coroutine.Sleep(300);
            WoWMovement.MoveStop();
            await Coroutine.Sleep(200);
            float facing = WoWMathHelper.CalculateNeededFacing(startPos, jumpAtLocation);
            StyxWoW.Me.SetFacing(facing + (float)(Rnd.NextDouble() - 0.5) * 2f * 0.1f);
            await Coroutine.Sleep(400);
            WoWMovement.Move(WoWMovement.MovementDirection.Forward);
            try
            {
                WoWPoint previousLocation = (WoWMovement.ActiveMover ?? StyxWoW.Me).Location;
                int timeout = Math.Max((int)(startPos.Distance(jumpAtLocation) / 7f * 1000f) + 2000, 5000);
                if (await Coroutine.Wait(timeout, () => ShouldJump(ref previousLocation, jumpAtLocation)))
                {
                    WoWMovement.Move(WoWMovement.MovementDirection.JumpAscend);
                    await Coroutine.Sleep(200);
                    WoWMovement.MoveStop(WoWMovement.MovementDirection.JumpAscend);
                }
            }
            finally
            {
                if (stopRunning)
                    WoWMovement.MoveStop(WoWMovement.MovementDirection.Forward);
            }
        }

        private static bool ShouldJump(ref WoWPoint previousLocation, WoWPoint jumpAtLocation)
        {
            WoWPoint location = (WoWMovement.ActiveMover ?? StyxWoW.Me).Location;
            float previousDistanceSquared = previousLocation.DistanceSqr(jumpAtLocation);
            float currentDistanceSquared = location.DistanceSqr(jumpAtLocation);
            if (currentDistanceSquared > previousDistanceSquared)
                return true;
            Vector3 previousDirection = previousLocation - jumpAtLocation;
            Vector3 currentDirection = location - jumpAtLocation;
            if (Vector3.Dot(previousDirection, currentDirection) < 0f)
                return true;
            float tickDistance = 7f / GameStats.TicksPerSecond;
            if (location.DistanceSqr(jumpAtLocation) < tickDistance + 0.5f)
                return true;
            if (Navigator.NavigationProvider is MeshNavigator meshNavigator)
            {
                WoWPoint pointInFront = WoWMathHelper.CalculatePointInFront(location, StyxWoW.Me.Rotation, tickDistance + 0.5f);
                lock (meshNavigator.Nav.MeshLock)
                {
                    meshNavigator.Nav.LoadTile(Tripper.MeshMisc.TileIdentifier.GetByPosition(location));
                    meshNavigator.Nav.LoadTile(Tripper.MeshMisc.TileIdentifier.GetByPosition(pointInFront));
                    if (meshNavigator.Nav.MeshQuery.FindNearestPolygon(Tripper.Navigation.NavHelper.ToNav(location), meshNavigator.Nav.Extents,
                        meshNavigator.Nav.QueryFilter.InternalFilter, out var nearestPoint, out var polygon).Succeeded && polygon.Id != 0 &&
                        meshNavigator.Nav.MeshQuery.Raycast(polygon, nearestPoint, Tripper.Navigation.NavHelper.ToNav(pointInFront),
                            meshNavigator.Nav.QueryFilter.InternalFilter, 256, out var _, out var distance, out var _).Succeeded &&
                        distance >= 0.01f && distance < 1f)
                        return true;
                }
            }
            previousLocation = location;
            return false;
        }

        /// <summary>
        ///     Jumps from one spot to another. Pet classes will dismiss pets 1st. Everyone Always Jumps if the condition is true.
        /// </summary>
        /// <param name="canRun"> The condition. </param>
        /// <param name="dismissPet">
        ///     if set to <c>true</c> if dismissing pet is needed.
        /// </param>
        /// <param name="from"> From. </param>
        /// <param name="to"> To. </param>
        /// <returns> </returns>
        public static Composite CreateForceJump(CanRunDecoratorDelegate canRun, bool dismissPet, WoWPoint from, WoWPoint to)
        {
            WoWPoint startLocation = WoWPoint.Zero;
            return
                new PrioritySelector(
                    new Decorator(
                        ctx => canRun(ctx),
                        new Sequence(
                            new DecoratorContinue(ctx => StyxWoW.Me.Location.DistanceSqr(from) > 5 * 5, CreateMoveToContinue(context => from)),
                // hunter dismiss pet
                            new DecoratorContinue(
                                ctx => dismissPet && StyxWoW.Me.Class == WoWClass.Hunter && StyxWoW.Me.GotAlivePet,
                                new Sequence(
                                    new DecoratorContinue(
                                        ctx => StyxWoW.Me.IsMoving,
                                        new Sequence(new Action(ctx => WoWMovement.MoveStop()), new WaitContinue(2, ctx => !StyxWoW.Me.IsMoving, new ActionAlwaysSucceed()))),
                                    new Action(ctx => SpellManager.Cast("Dismiss Pet")),
                                    new WaitContinue(3, ctx => false, new ActionAlwaysSucceed()))),
                // nonhunter dismiss pet.
                            new DecoratorContinue(
                                ctx => dismissPet && StyxWoW.Me.Class != WoWClass.Hunter && StyxWoW.Me.GotAlivePet, new Action(ctx => Lua.DoString("PetDismiss()"))),
                            new Action(ctx => WoWMovement.ClickToMove(to)),
                            new Action(ctx => startLocation = StyxWoW.Me.Location),
                            new Sleep(100),
                            new WaitContinue(TimeSpan.FromMilliseconds(2000), ctx => ShouldJump(ref startLocation, from), new ActionAlwaysSucceed()),
                            new Action(ctx => WoWMovement.Move(WoWMovement.MovementDirection.JumpAscend)),
                            new WaitContinue(2, ctx => false, new ActionAlwaysSucceed()),
                            new Action(ctx => WoWMovement.MoveStop(WoWMovement.MovementDirection.JumpAscend)))));
        }

        /// <summary>
        ///     Moves the tank to a location only if party is ready to move and also clears a path while moving to location.
        /// </summary>
        /// <param name="loc"> The loc. </param>
        /// <returns> </returns>
        [Obsolete("Use SetTankMovetoPOI instead")]
        public static RunStatus MoveTankTo(WoWPoint loc)
        {
            return SetLeaderMoveToPoi(loc) ? RunStatus.Success : RunStatus.Failure;
        }

        /// <summary>
        ///     Moves to a location. For use with a Sequence
        /// </summary>
        /// <param name="objectId"> The object id. </param>
        /// <returns> </returns>
        public static Composite CreateMoveToContinue(uint objectId)
        {
            return CreateMoveToContinue(ctx => true, objectId, true);
        }

        /// <summary>
        ///     Moves to a location. For use with a Sequence
        /// </summary>
        /// <param name="objectSelector"> The object selector. </param>
        /// <returns> </returns>
        public static Composite CreateMoveToContinue(Func<object, WoWObject> objectSelector)
        {
            return CreateMoveToContinue(ctx => true, objectSelector, true);
        }

        /// <summary>
        ///     Moves to a location. For use with a Sequence
        /// </summary>
        /// <param name="canRun"> The can run. </param>
        /// <param name="objectId"> Object Id to move to </param>
        /// <param name="ignoreCombat"> Keeps moving even when in combat </param>
        /// <returns> </returns>
        public static Composite CreateMoveToContinue(CanRunDecoratorDelegate canRun, uint objectId, bool ignoreCombat)
        {
            return CreateMoveToContinue(canRun, context => ObjectManager.ObjectList.Where(o => o.Entry == objectId).OrderBy(o => o.DistanceSqr).FirstOrDefault(), ignoreCombat);
        }

        /// <summary>
        ///     Moves to a location. For use with a Sequence
        /// </summary>
        /// <param name="canRun"> The can run. </param>
        /// <param name="objectSelector"> The object selector. </param>
        /// <param name="ignoreCombat"> Keeps moving even when in combat </param>
        /// <returns> </returns>
        public static Composite CreateMoveToContinue(CanRunDecoratorDelegate canRun, Func<object, WoWObject> objectSelector, bool ignoreCombat)
        {
            return CreateMoveToContinue(
                canRun,
                context =>
                {
                    var obj = objectSelector(context);
                    return obj != null ? obj.Location : WoWPoint.Zero;
                },
                ignoreCombat);
        }

        /// <summary>
        ///     Moves to a location. For use with a Sequence
        /// </summary>
        /// <param name="locationSelector"> The location selector. </param>
        /// <returns> </returns>
        public static Composite CreateMoveToContinue(Func<object, WoWPoint> locationSelector)
        {
            return CreateMoveToContinue(ctx => true, locationSelector, true);
        }

        /// <summary>
        ///     Moves to a location. For use with a Sequence
        /// </summary>
        /// <param name="canRun"> The can run. </param>
        /// <param name="locationSelector"> Location to move to </param>
        /// <param name="ignoreCombat"> Keeps moving even when in combat </param>
        /// <returns> </returns>
        public static Composite CreateMoveToContinue(CanRunDecoratorDelegate canRun, Func<object, WoWPoint> locationSelector, bool ignoreCombat)
        {
            return new Action(
                ctx =>
                {
                    var location = locationSelector(ctx);
                    if ((!ignoreCombat && StyxWoW.Me.IsActuallyInCombat) || !StyxWoW.Me.IsAlive || location == WoWPoint.Zero)
                        return RunStatus.Failure;
                    if (StyxWoW.Me.Location.DistanceSqr(location) <= 4 * 4 || !canRun(ctx))
                        return RunStatus.Success;
                    var moveStatus = Navigator.MoveTo(location);
                    switch (moveStatus)
                    {
                        case MoveResult.Failed:
                        case MoveResult.PathGenerationFailed:
                            return RunStatus.Failure;
                        default:
                            return RunStatus.Running;
                    }
                });
        }

        /// <summary>
        ///     Gets a point behind unit.
        /// </summary>
        /// <param name="unitSelector"> The unit. </param>
        /// <param name="distance"> How far behind to go </param>
        /// <returns> </returns>
        private static WoWPoint CalculatePointBehindUnit(WoWUnit unit)
        {
            WoWPoint segmentStart = WoWMathHelper.CalculatePointBehind(unit.Location, unit.Rotation, 3f);
            WoWPoint segmentEnd = WoWMathHelper.CalculatePointBehind(unit.Location, unit.Rotation, unit.MeleeRange());
            return StyxWoW.Me.Location.GetNearestPointOnSegment(segmentStart, segmentEnd);
        }

        /// <summary>
        ///     Gets behind a given unit
        /// </summary>
        /// <param name="canRun"> The can run. </param>
        /// <param name="who"> The who. </param>
        /// <param name="distance"> How far behind to go </param>
        /// <returns> </returns>
        public static Composite GetBehindUnit(CanRunDecoratorDelegate canRun, Func<object, WoWUnit> targetProducer)
        {
            return new Decorator(canRun, new Action(context => Navigator.MoveTo(CalculatePointBehindUnit(targetProducer(context)))));
        }

        /// <summary>
        ///     Gets the spread out location.
        /// </summary>
        /// <param name="stepDistance"> The step distance. </param>
        /// <param name="centerPoint"> The center point. </param>
        /// <param name="directionToHead"> The direction to head. </param>
        /// <param name="maxDistanceFromCenterPoint"> The max distance from center point. </param>
        /// <returns> </returns>
        public static WoWPoint GetSpreadOutLocation(float stepDistance, WoWPoint centerPoint, float directionToHead, float maxDistanceFromCenterPoint)
        {
            float maxDistanceFromCenterPointSqr = maxDistanceFromCenterPoint * maxDistanceFromCenterPoint;
            WoWPoint moveFromPoint = StyxWoW.Me.Location;
            for (int angle = 0; angle < 180; angle = -angle + (angle >= 0 ? -20 : 0))
            {
                //  generate a point in the direction where we want to go. 
                var point = WoWMathHelper.GetPointAt(moveFromPoint, stepDistance, directionToHead + WoWMathHelper.DegreesToRadians(angle), 0);
                // check if we will pull aggro if we move to our point
                if (WillPullAggroAtLocation(point))
                    continue;
                // don't move too far from center point
                if (point.DistanceSqr(centerPoint) > maxDistanceFromCenterPointSqr)
                    continue;
                // we have found a point that we can move to.
                if (Navigator.CanNavigateFully(moveFromPoint, point))
                    return point;
            }
            return WoWPoint.Zero;
        }

        /// <summary>
        ///     This behavior will run out of Line-Of-Sight of a location.
        /// </summary>
        /// <param name="canRun"> The condition to run this behavior </param>
        /// <param name="locationToLos"> The location you want to get out of LOS with. </param>
        /// <param name="objectLocationToLosAt"> The center location of the object you want to use to LOS </param>
        /// <param name="objectRadius"> The radius of the object you want to use to LOS </param>
        /// <returns> </returns>
        public static Composite CreateLosLocation(CanRunDecoratorDelegate canRun, Func<object, WoWPoint> locationToLos, Func<object, WoWPoint> objectLocationToLosAt, Func<object, float> objectRadius)
        {
            return new Decorator(canRun, new ActionRunCoroutine(context => MoveOutOfLos(
                () => canRun(context), locationToLos(context), objectLocationToLosAt(context), objectRadius(context))));
        }

        /// <summary>
        ///     Tanks unit against an object e.g it could be a pillar.
        /// </summary>
        /// <param name="canRun"> The can run. </param>
        /// <param name="objectLocationToTankAt"> The object location to tank at. </param>
        /// <param name="objectRadius"> The object radius. </param>
        /// <returns> </returns>
        public static Composite CreateTankAgainstObject(CanRunDecoratorDelegate canRun, Func<object, WoWPoint> objectLocationToTankAt, Func<object, float> objectRadius)
        {
            return new Decorator(canRun, new ActionRunCoroutine(context => TankAgainstObject(objectLocationToTankAt(context), objectRadius(context))));
        }


        /// <summary>
        ///     Pull NPC to location.
        /// </summary>
        /// <param name="canRun"> The 'can run' condition. </param>
        /// <param name="unitSelector"> The unit selector. </param>
        /// <param name="pullToLocationSelector"> The pull to location selector. if null then current locaton is used </param>
        /// <param name="waitTimeAtPullLocationTimeout"> The max time in seconds to wait at pull location for target to come in melee range. </param>
        /// <returns> </returns>
        public static Composite CreatePullNpcToLocation(
            CanRunDecoratorDelegate canRun, Func<object, WoWUnit> unitSelector, Func<object, WoWPoint> pullToLocationSelector, int waitTimeAtPullLocationTimeout)
        {
            return CreatePullNpcToLocation(canRun, ctx => true, unitSelector, pullToLocationSelector, null, waitTimeAtPullLocationTimeout);
        }

        /// <summary>
        ///     Pull NPC to location.
        /// </summary>
        /// <param name="canRun"> The 'can run' condition. </param>
        /// <param name="readyToPull"> The 'ready to pull' condition. </param>
        /// <param name="unitSelector"> The unit selector. </param>
        /// <param name="pullToLocationSelector"> The pull to location selector. if null then current locaton is used </param>
        /// <param name="waitTimeAtPullLocationTimeout"> The max time in seconds to wait at pull location for target to come in melee range. </param>
        /// <returns> </returns>
        public static Composite CreatePullNpcToLocation(
            CanRunDecoratorDelegate canRun,
            CanRunDecoratorDelegate readyToPull,
            Func<object, WoWUnit> unitSelector,
            Func<object, WoWPoint> pullToLocationSelector,
            int waitTimeAtPullLocationTimeout)
        {
            return CreatePullNpcToLocation(canRun, readyToPull, unitSelector, pullToLocationSelector, null, waitTimeAtPullLocationTimeout);
        }

        /// <summary>
        ///     Pull NPC to location.
        /// </summary>
        /// <param name="canRun"> The 'can run' condition. </param>
        /// <param name="readyToPull"> The 'ready to pull' condition. </param>
        /// <param name="unitSelector"> The unit selector. </param>
        /// <param name="pullToLocationSelector"> The pull to location selector. if null then current locaton is used </param>
        /// <param name="waitAtLocationSellector"> The 'wait at' location sellector. if null then current locaton is used </param>
        /// <returns> </returns>
        public static Composite CreatePullNpcToLocation(
            CanRunDecoratorDelegate canRun,
            CanRunDecoratorDelegate readyToPull,
            Func<object, WoWUnit> unitSelector,
            Func<object, WoWPoint> pullToLocationSelector,
            Func<object, WoWPoint> waitAtLocationSellector)
        {
            return CreatePullNpcToLocation(canRun, ctx => true, unitSelector, pullToLocationSelector, null, 0);
        }

        public static Composite CreateRunToTankIfAggroed()
        {
            return new PrioritySelector(
                new Decorator(
                    context => StyxWoW.Me.Combat && StyxWoW.Me.IsFollower() &&
                        ObjectManager.GetObjectsOfTypeFast<WoWUnit>().Any(unit => unit.ThreatInfo.ThreatStatus >= ThreatStatus.NoobishTank),
                    new PrioritySelector(
                        context => GroupMembers.Where(member => member.IsTank && member.MapId == StyxWoW.Me.MapId)
                            .OrderByDescending(member => member.MaxHealth).FirstOrDefault(),
                        new Decorator<GroupMember>(
                            member => member.Location.DistanceSqr(StyxWoW.Me.Location) > 100f,
                            new PrioritySelector(
                                new ActionSetActivity("Running to Tank because I have aggro"),
                                new Action<GroupMember>(member => { Navigator.MoveTo(member.Location); }))),
                        new Decorator<GroupMember>(member =>
                        {
                            if (StyxWoW.Me.IsHealer())
                                return false;
                            WoWUnit target = Targeting.Instance.FirstUnit;
                            if (target == null)
                                return false;
                            float range = StyxWoW.Me.IsRange() ? target.CombatReach + 1f + 40f : target.MeleeRange();
                            return member.Location.DistanceSqr(target.Location) >= (range + 10f) * (range + 10f);
                        }, new ActionAlwaysSucceed()))));
        }

        /// <summary>
        ///     Pull NPC to location.
        /// </summary>
        /// <param name="canRun"> The 'can run' condition. </param>
        /// <param name="readyToPull"> The 'ready to pull' condition. </param>
        /// <param name="unitSelector"> The unit selector. </param>
        /// <param name="pullToLocationSelector"> The pull to location selector. if null then current locaton is used </param>
        /// <param name="waitAtLocationSellector"> The 'wait at' location sellector. if null then current locaton is used </param>
        /// <param name="waitTimeAtPullLocationTimeout"> The max time in seconds to wait at pull location for target to come in melee range. </param>
        /// <returns> </returns>
        public static Composite CreatePullNpcToLocation(
            CanRunDecoratorDelegate canRun,
            CanRunDecoratorDelegate readyToPull,
            Func<object, WoWUnit> unitSelector,
            Func<object, WoWPoint> pullToLocationSelector,
            Func<object, WoWPoint> waitAtLocationSellector,
            int waitTimeAtPullLocationTimeout)
        {
            return new Decorator(canRun, new ActionRunCoroutine(context => PullNpcToLocation(
                () => canRun(context),
                () => readyToPull(context),
                unitSelector(context),
                pullToLocationSelector != null ? new WoWPoint?(pullToLocationSelector(context)) : null,
                waitAtLocationSellector != null ? new WoWPoint?(waitAtLocationSellector(context)) : null,
                waitTimeAtPullLocationTimeout * 1000)));
        }

        public static async Task<bool> TankUnitAtLocation(WoWPoint location, float precision)
        {
            if (!StyxWoW.Me.IsTank() || !StyxWoW.Me.IsActuallyInCombat)
                return false;
            WoWUnit target = StyxWoW.Me.CurrentTarget;
            if (target == null || target.CurrentTargetGuid != StyxWoW.Me.Guid || !target.IsHostile)
                return false;
            if (StyxWoW.Me.Location.DistanceSqr(location) > precision * precision)
            {
                MoveResult result = await CommonCoroutines.MoveTo(location, string.Format("Tank spot at {0}", location));
                return result.IsSuccessful();
            }
            return !target.IsWithinMeleeRange;
        }

        public static async Task<bool> AvoidUnitAngles(WoWUnit unit, params AngleSpan[] angleSpans)
        {
            if (unit == null || !unit.IsValid)
                return false;
            WoWPoint destination;
            if (!IsInUnitAngleSpans(unit, angleSpans) || !GetPointOutsideUnitAngleSpans(unit, out destination, angleSpans))
                return false;
            string spans = string.Join(" | ", angleSpans.Select(angleSpan => angleSpan.ToString()));
            Logger.Write("Moving out of cone area: [{0}] on unit: {1}", spans, unit.SafeName);
            MoveResult result = await CommonCoroutines.MoveTo(destination);
            return result.IsSuccessful();
        }

        public static async Task<bool> StopMovingIfMoving(string reason = null)
        {
            WoWUnit activeMover = WoWMovement.ActiveMover;
            if (activeMover == null || !activeMover.IsMoving)
                return false;
            WoWMovement.MoveStop();
            if (!string.IsNullOrEmpty(reason))
                Logger.Write("Stopped moving. Reason: {0}", reason);
            return await Coroutine.Wait(4000, () => !activeMover.IsMoving);
        }

        public static async Task<bool> FaceIfNotFacing(WoWUnit target, string reason = null)
        {
            if (!IsViable(target))
                return false;
            WoWUnit activeMover = WoWMovement.ActiveMover;
            if (activeMover == null || activeMover.IsSafelyFacing(target))
                return false;
            target.Face();
            if (!string.IsNullOrEmpty(reason))
                Logger.Write("facing {0}. Reason: {1}", target.SafeName, reason);
            return await Coroutine.Wait(4000, () => activeMover.IsSafelyFacing(target));
        }

        public static async Task<bool> GetBehindUnit(WoWUnit unit)
        {
            MoveResult result = await CommonCoroutines.MoveTo(CalculatePointBehindUnit(unit), string.Format("get behind {0}", unit.SafeName));
            return result.IsSuccessful();
        }

        public static async Task<bool> MoveOutOfLos(Func<bool> losWhile, WoWPoint locationToLos, WoWPoint objectLocationToLosAt, float objectRadius)
        {
            if (!losWhile())
                return false;
            WoWPoint location = WoWMathHelper.CalculatePointFrom(locationToLos, objectLocationToLosAt, 0f - objectRadius);
            if (!Navigator.AtLocation(location))
            {
                MoveResult result = await CommonCoroutines.MoveTo(location, "LOS location");
                return result.IsSuccessful();
            }
            if (MovementEnabled)
                DisableMovement(losWhile);
            return false;
        }

        private static async Task<bool> TankAgainstObject(WoWPoint objectLocation, float objectRadius)
        {
            if (!StyxWoW.Me.IsTank() || !StyxWoW.Me.IsActuallyInCombat)
                return false;
            WoWUnit target = StyxWoW.Me.CurrentTarget;
            if (target == null || !target.Aggro || !target.IsHostile)
                return false;
            WoWPoint location = WoWMathHelper.CalculatePointFrom(target.Location, objectLocation, objectRadius);
            if (!Navigator.AtLocation(location))
            {
                MoveResult result = await CommonCoroutines.MoveTo(location, string.Format("tank current target against object at location {0}", objectLocation));
                return result.IsSuccessful();
            }
            return !target.IsWithinMeleeRange;
        }

        public static async Task<bool> SpreadOut(WoWPoint center, int distanceToSpread, float maxDistanceFromCenterPoint)
        {
            if (StyxWoW.Me.IsMelee())
                return false;
            WoWPoint myLocation = StyxWoW.Me.Location;
            int spreadDistanceSquared = distanceToSpread * distanceToSpread;
            WoWPoint destination;
            if (StyxWoW.Me.IsDps())
            {
                List<WoWPlayer> members = StyxWoW.Me.RaidMembers.Where(player => !player.IsMe && player.DistanceSqr <= spreadDistanceSquared)
                    .OrderBy(player => player.DistanceSqr).ToList();
                if (!members.Any())
                    return false;
                float[] directions = members.Select(member => member.Location.GetDirectionTo(myLocation))
                    .Select(point => (float)Math.Atan2(point.Y, point.X)).ToArray();
                float direction = (directions.Max() + directions.Min()) / 2f;
                float stepDistance = distanceToSpread + 1 - (float)members[0].Distance;
                float random = 0.4f - (float)Rnd.NextDouble() * 0.8f;
                destination = GetSpreadOutLocation(stepDistance, center, direction + random, maxDistanceFromCenterPoint);
            }
            else
            {
                List<WoWPlayer> members = StyxWoW.Me.RaidMembers
                    .Where(player => player.Location.DistanceSqr(Tank.Location) <= 49f && player.DistanceSqr <= spreadDistanceSquared)
                    .OrderBy(player => player.DistanceSqr).ToList();
                if (!members.Any())
                    return false;
                float direction = members.Select(member => member.Location.GetDirectionTo(myLocation))
                    .Select(point => (float)Math.Atan2(point.Y, point.X)).Average();
                float stepDistance = distanceToSpread + 1 - (float)members[0].Distance;
                destination = GetSpreadOutLocation(stepDistance, center, direction, maxDistanceFromCenterPoint);
            }
            if (destination != WoWPoint.Zero && Navigator.CanNavigateFully(myLocation, destination))
            {
                MoveResult result = await CommonCoroutines.MoveTo(destination, "Spread out");
                return result.IsSuccessful();
            }
            Logger.Write("failed to spread out");
            return false;
        }

        public static async Task<bool> PullNpcToLocation(Func<bool> canRun, WoWUnit unit, WoWPoint? pullToLocation, int waitTimeAtPullLocationTimeoutMs)
        {
            return await PullNpcToLocation(canRun, () => true, unit, pullToLocation, null, waitTimeAtPullLocationTimeoutMs);
        }

        public static async Task<bool> PullNpcToLocation(Func<bool> canRun, Func<bool> readyToPull, WoWUnit unit, WoWPoint? pullToLocation, WoWPoint? waitAtLocation, int waitTimeAtPullLocationTimeoutMs)
        {
            if (!canRun() || StyxWoW.Me.PartyMembers.Any(player => player.IsDead) || !IsViable(unit))
                return false;
            if (StyxWoW.Me.IsTank() && !StyxWoW.Me.Combat)
            {
                if (StyxWoW.Me.HealthPercent <= 70.0)
                {
                    TreeRoot.StatusText = "Waiting for heals before pulling.";
                    return true;
                }
                if (!GroupMembers.All(member => member.IsAlive && member.Location.DistanceSqr(StyxWoW.Me.Location) <= 1600f))
                {
                    TreeRoot.StatusText = "Waiting for group members to move closer before pulling.";
                    return true;
                }
                if (readyToPull())
                {
                    if (StyxWoW.Me.CurrentTargetGuid != unit.Guid)
                    {
                        unit.Target();
                        await CommonCoroutines.SleepForLagDuration();
                        return true;
                    }
                    WoWPoint pullLocation = pullToLocation.HasValue ? pullToLocation.Value : StyxWoW.Me.Location;
                    try
                    {
                        TreeRoot.StatusText = "Pulling " + unit.SafeName;
                        if (!await MoveToContinue(() => unit.Location, () => canRun() && readyToPull() && GetRangedSpell(unit) == null, false, unit.SafeName))
                            return false;
                        if ((canRun() && readyToPull() && GetRangedSpell(unit) != null) || StyxWoW.Me.Combat)
                        {
                            if (!StyxWoW.Me.Combat && (!await CastRangedAbility(unit) || !await Coroutine.Wait(4000, () => unit.Aggro)))
                                return false;
                            if (!await MoveToContinue(() => pullLocation, null, true, "Pull location"))
                                return false;
                            await StopMovingIfMoving();
                            await FaceIfNotFacing(unit);
                            if (!await Coroutine.Wait(waitTimeAtPullLocationTimeoutMs, () => unit.IsWithinMeleeRange || !unit.Aggro))
                                return false;
                        }
                    }
                    finally
                    {
                        TreeRoot.StatusText = null;
                    }
                    return true;
                }
                TreeRoot.StatusText = string.Format("Waiting to pull");
                if (waitAtLocation.HasValue && !Navigator.AtLocation(waitAtLocation.Value))
                {
                    MoveResult result = await CommonCoroutines.MoveTo(waitAtLocation.Value);
                    return result.IsSuccessful();
                }
                await StopMovingIfMoving();
                return true;
            }
            if (StyxWoW.Me.IsFollower())
                return canRun() && await StayAtLocationUntilTankPulls(waitAtLocation);
            return false;
        }

        private static readonly WaitTimer MovementLogTimer = WaitTimer.OneSecond;
        private static WoWPoint _lastMovementLogLocation;

        private static bool ShouldLogMovement(WoWPoint location)
        {
            if (location.DistanceSqr(_lastMovementLogLocation) > 25f && MovementLogTimer.IsFinished)
            {
                MovementLogTimer.Reset();
                _lastMovementLogLocation = location;
                return true;
            }
            return false;
        }

        public static async Task<bool> MoveTo(WoWPoint point, string name = "")
        {
            if (MovementLogTimer.IsFinished && !string.IsNullOrEmpty(name))
            {
                Logger.Write("Moving to {0}", name);
                MovementLogTimer.Reset();
            }
            return Navigator.MoveTo(point).IsSuccessful();
        }

        public static async Task<bool> MoveToContinue(Func<WoWPoint> destinationProducer, Func<bool> whileTrue = null, bool ignoreCombat = false, string name = "")
        {
            while (true)
            {
                WoWPoint destination = destinationProducer();
                if (!ignoreCombat && StyxWoW.Me.IsActuallyInCombat || !StyxWoW.Me.IsAlive || destination == WoWPoint.Zero)
                    return false;
                MoveResult result = await CommonCoroutines.MoveTo(destination, name);
                if (!result.IsSuccessful())
                    return false;
                if (Navigator.AtLocation(destination) || whileTrue != null && !whileTrue())
                    return true;
                await Coroutine.Yield();
            }
        }

        public static async Task<bool> StayAtLocationUntilTankPulls(WoWPoint? location, float maxDistance = 4f)
        {
            if (!StyxWoW.Me.IsFollower() || !ShouldWaitForTankToPull)
                return false;
            if (location.HasValue && !Navigator.AtLocation(location.Value))
            {
                MoveResult result = await CommonCoroutines.MoveTo(location.Value);
                return result.IsSuccessful();
            }
            if (ShouldLogMovement(location.HasValue ? location.Value : StyxWoW.Me.Location))
                Logger.WriteDebug("Waiting for tank to pull");
            await StopMovingIfMoving();
            return true;
        }

        private static bool CanReachUnitFromCircle(WoWUnit unit, WoWPoint center, float radius)
        {
            if (!IsViable(unit))
                return false;
            float range = StyxWoW.Me.IsMelee() ? unit.MeleeRange() + radius : unit.CombatReach + 35f + radius;
            return center.Distance(unit.Location) < range && unit.InLineOfSpellSight;
        }

        public static async Task<bool> StayAtLocationWhile(Func<bool> condition, WoWPoint location, string locationDescription = null, float precision = 8f, params GroupRoleFlags[] roles)
        {
            if (roles.Any() && !roles.Any(role => StyxWoW.Me.HasRole(role)) || !condition())
                return false;
            WoWUnit target = StyxWoW.Me.IsHealer() ? HealTargeting.Instance.FirstUnit : Targeting.Instance.FirstUnit;
            if (!IsViable(target))
                return false;
            WoWPoint myLocation = StyxWoW.Me.Location;
            if (myLocation.DistanceSqr(location) > precision * precision)
            {
                if (!MovementEnabled)
                    RestoreMovement();
                MoveResult result = await CommonCoroutines.MoveTo(location, locationDescription);
                return result.IsSuccessful();
            }
            if (ShouldLogMovement(location))
            {
                string description = !string.IsNullOrEmpty(locationDescription) ? locationDescription : location.ToString();
                Logger.WriteDebug("Waiting at {0}", description);
            }
            bool isHealer = StyxWoW.Me.IsHealer();
            if (isHealer || !CanReachUnitFromCircle(target, location, precision) && !AvoidanceManager.IsRunningOutOfAvoid)
            {
                if (MovementEnabled)
                {
                    if (isHealer)
                    {
                        DisableMovement(() => condition() && !AvoidanceManager.IsRunningOutOfAvoid &&
                            StyxWoW.Me.Location.DistanceSqr(location) <= precision * precision);
                    }
                    else
                    {
                        DisableMovement(() => condition() && !CanReachUnitFromCircle(Targeting.Instance.FirstUnit, location, precision) &&
                            !AvoidanceManager.IsRunningOutOfAvoid && StyxWoW.Me.Location.DistanceSqr(location) <= precision * precision);
                    }
                }
                Navigator.NavigationProvider.StuckHandler.Reset();
            }
            return false;
        }

        public static RunStatus SetLeaderMoveToPoiPS(WoWPoint destination, string destinationName = "")
        {
            return SetLeaderMoveToPoi(destination, destinationName) ? RunStatus.Success : RunStatus.Failure;
        }

        [Obsolete("Use SetLeaderMoveToPoi")]
        public static bool SetTankMoveToPoi(WoWPoint destination, string destinationName = "")
        {
            return SetLeaderMoveToPoi(destination, destinationName);
        }

        public static bool SetLeaderMoveToPoi(WoWPoint destination, string destinationName = null)
        {
            if (StyxWoW.Me.IsTank() && !BotPoi.Current.Type.IsType(PoiType.Kill, PoiType.Repair, PoiType.Sell, PoiType.Train,
                PoiType.Mail, PoiType.Loot, PoiType.Interact, PoiType.Harvest, PoiType.Skin))
            {
                if (BotPoi.Current.Type != PoiType.Hotspot || BotPoi.Current.Location.DistanceSqr(destination) > 1f)
                {
                    if (StyxWoW.Me.Location.DistanceSqr(destination) >= 16f)
                    {
                        BotPoi.Current = new BotPoi(destination, PoiType.Hotspot);
                        Logger.Write("Tank MoveTo POI set to {0}", destinationName ?? destination.ToString());
                        return true;
                    }
                    if (BotPoi.Current.Type == PoiType.Hotspot)
                        BotPoi.Clear();
                }
            }
            return false;
        }

        public static bool SetInteractPoi(WoWObject interactObject)
        {
            if (!IsViable(interactObject))
                return false;
            if (BotPoi.Current.Type.IsType(PoiType.Kill, PoiType.Repair, PoiType.Sell, PoiType.Train, PoiType.Mail,
                PoiType.Loot, PoiType.Harvest, PoiType.Skin))
                return false;
            if (BotPoi.Current.Type == PoiType.Interact && BotPoi.Current.Guid == interactObject.Guid)
                return false;
            BotPoi.Current = new BotPoi(interactObject, PoiType.Interact);
            Logger.Write("Interact POI set to {0}", interactObject.SafeName);
            return true;
        }


        /// <summary>
        ///     This method checks if movement should be restored if it was disable. We need to pulse this from the main HB thread because of synchronization isues...
        /// </summary>
        internal static void PulseMovementChecks()
        {
            try
            {
                DisableMovementConditions.RemoveAll(cond => !cond.Condition() || !StyxWoW.Me.IsAlive);
                // remove all conditions that are 'false'.
                if (DisableMovementConditions.Any())
                // we should not restore movement since there are still some conditions that remain 'true'
                {
                    if (_controlledMoveToPoint != WoWPoint.Zero) // we need to go somewhere.
                    {
                        if (!MovementEnabled)
                            Navigator.PlayerMover = new ClickToMoveMover();
                        Navigator.MoveTo(_controlledMoveToPoint);
                        Navigator.PlayerMover = new NoMover();
                        Navigator.NavigationProvider.StuckHandler = new NoUnstuck();
                        // disable movement after we move so CC/Plugins can't go anywhere (unless they use ctm directly :P )
                    }
                    Navigator.NavigationProvider.StuckHandler.Reset();
                }
                else if (!MovementEnabled)
                {
                    RestoreMovement();
                    _controlledMoveToPoint = WoWPoint.Zero;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteDebug(ex.ToString());
            }
        }

        /// <summary>
        ///     Disables movement until expresion evaluates to 'false'. Use with care.
        /// </summary>
        /// <param name="whileTrue"> The while true. </param>
        public static void DisableMovement(Func<bool> whileTrue)
        {
            if (DisableMovementConditions.All(i => i.Condition != whileTrue))
                DisableMovementConditions.Add(new ControlledMoveToCondition(whileTrue, ControlledMoveToType.DisableMovement));
            if (MovementEnabled)
            {
                Navigator.PlayerMover = new NoMover();
                if (_originalStuckHandler == null)
                    _originalStuckHandler = Navigator.NavigationProvider.StuckHandler;
                Navigator.NavigationProvider.StuckHandler = new NoUnstuck();
                _controlledMoveToPoint = WoWPoint.Zero;
            }
        }

        /// <summary>
        ///     Restores movement.
        /// </summary>
        public static void RestoreMovement()
        {
            if (!MovementEnabled)
            {
                DisableMovementConditions.Clear();
                Navigator.PlayerMover = new ClickToMoveMover();
                Navigator.NavigationProvider.StuckHandler = _originalStuckHandler;
                _controlledMoveToPoint = WoWPoint.Zero;
            }
        }

        /// <summary>
        ///     This activates controlled movement and moves to a location without being interfered by movement from CC/plugins
        /// </summary>
        /// <param name="location"> The location to move to. </param>
        /// <returns> </returns>
        public static MoveResult ControlledMoveTo(WoWPoint location)
        {
            bool alreadyHeadingThere = location == _controlledMoveToPoint;
            if (MovementEnabled)
            {
                Navigator.PlayerMover = new ClickToMoveMover();
                if (_originalStuckHandler != null)
                    Navigator.NavigationProvider.StuckHandler = _originalStuckHandler;
            }
            var moveResult = Navigator.MoveTo(location);

            if (!alreadyHeadingThere)
            {
                DisableMovementConditions.RemoveAll(cond => cond.Type == ControlledMoveToType.MoveTo);
                // make sure we have no stale conditions lingering about
                DisableMovementConditions.Add(
                    new ControlledMoveToCondition(() => StyxWoW.Me.Location.DistanceSqr(location) > 4 * 4 && StyxWoW.Me.IsAlive, ControlledMoveToType.MoveTo));
            }
            Navigator.PlayerMover = new NoMover();
            if (_originalStuckHandler == null)
                _originalStuckHandler = Navigator.NavigationProvider.StuckHandler;
            Navigator.NavigationProvider.StuckHandler = new NoUnstuck();
            _controlledMoveToPoint = location;
            return moveResult;
        }

        public static Composite CreateAvoidUnitAnglesBehavior(CanRunDecoratorDelegate canRun, Func<object, WoWUnit> unitSelector, params AngleSpan[] angleSpans)
        {
            return new Decorator(canRun, new ActionRunCoroutine(context => AvoidUnitAngles(unitSelector(context), angleSpans)));
        }

        public static bool IsInUnitAngleSpans(WoWObject unit, params AngleSpan[] angleSpans)
        {
            return IsLocationInAngleSpans(StyxWoW.Me.Location, unit.Location, unit.Rotation, angleSpans);
        }

        public static bool IsLocationInAngleSpans(WoWPoint location, WoWPoint rayOrigin, float rayRotation, params AngleSpan[] angleSpans)
        {
            rayRotation = WoWMathHelper.NormalizeRadian(rayRotation);
            WoWPoint offset = location - rayOrigin;
            float locationAngle = WoWMathHelper.NormalizeRadian((float)Math.Atan2(offset.Y, offset.X));
            float rotationDifference = WoWMathHelper.RadiansToDegrees(rayRotation > locationAngle ? PIx2 - (rayRotation - locationAngle) : locationAngle - rayRotation);

            foreach (var angleSpan in angleSpans)
            {
                var halfSpan = angleSpan.Span / 2f;
                float minAngle = angleSpan.Angle - halfSpan;
                if (minAngle < 0) minAngle += 360;
                float maxAngle = (angleSpan.Angle + halfSpan) % 360;

                if (minAngle < maxAngle ? minAngle <= rotationDifference && rotationDifference <= maxAngle : rotationDifference >= minAngle || rotationDifference <= maxAngle)
                    return true;
            }
            return false;
        }

        private static bool GetPointOutsideUnitAngleSpans(WoWUnit unit, out WoWPoint moveTo, params AngleSpan[] angleSpans)
        {
            float maxRange = StyxWoW.Me.IsRange() ? unit.CombatReach + 35f : unit.MeleeRange();
            return GetPointOutsideAngleSpans(StyxWoW.Me.Location, unit.Location, unit.Rotation, maxRange, out moveTo, angleSpans);
        }

        public static bool GetPointOutsideAngleSpans(WoWPoint start, WoWPoint rayOrigin, float rayRotation, float maxRange, out WoWPoint outsidePoint, params AngleSpan[] angleSpans)
        {
            float rotation = WoWMathHelper.RadiansToDegrees(WoWMathHelper.NormalizeRadian(rayRotation));
            const int minRange = 4;

            // generate a hashset of angles ranging from 5 to 360 with step 5
            var angles = new HashSet<int>(Range<int>(10, 360, 10));

            // remove all angles that we need to avoid.
            foreach (var angleSpan in angleSpans)
            {
                var halfSpan = angleSpan.Span / 2f;
                var realAng = angleSpan.Angle + rotation;
                float minAngle = (realAng % 360) - halfSpan;
                if (minAngle < 0) minAngle += 360;
                float maxAngle = (realAng + halfSpan) % 360;

                if (minAngle < maxAngle)
                    angles.RemoveWhere(ang => ang >= minAngle && ang <= maxAngle);
                else
                    angles.RemoveWhere(ang => ang >= minAngle || ang <= maxAngle);
            }
            // generate the points and order by nearest point.
            var pointCollection = angles.Aggregate(
                new List<WoWPoint>(),
                (points, ang) =>
                {
                    points.AddRange(Range<float>(minRange, maxRange, 5f).Select(dist => rayOrigin.RayCast(WoWMathHelper.DegreesToRadians(ang), dist)));
                    return points;
                }).OrderBy(p => p.DistanceSqr(start)).ToArray();
            // find the nearest point that we can navigate to and isn't inside an avoid.
            outsidePoint = (from point in pointCollection
                      where !AvoidanceManager.Avoids.Any(a => a.IsPointInAvoid(point))
                      let zList = Navigator.FindHeights(point.X, point.Y)
                      where zList != null && zList.Count > 0
                      // set z to nearest mesh z.
                      let newPoint = new WoWPoint(point.X, point.Y, zList.OrderBy(z => Math.Abs(z - point.Z)).FirstOrDefault())
                      where newPoint.DistanceSqr(start) > 4f && Navigator.CanNavigateFully(start, newPoint)
                      select newPoint).FirstOrDefault();

            return outsidePoint != WoWPoint.Zero;
        }

        private static IEnumerable<T> Range<T>(dynamic min, dynamic max, dynamic step) where T : struct
        {
            T i;
            for (i = min; i <= max; i += step)
                yield return i;

            if (i != max + step)
                yield return max;
        }

        #region Nested type: ControlledMoveToType

        internal enum ControlledMoveToType
        {
            MoveTo,
            DisableMovement
        }

        #endregion

        #region Embedded Type - ControlledMoveToCondition

        internal class ControlledMoveToCondition
        {
            public ControlledMoveToCondition(Func<bool> condition, ControlledMoveToType type)
            {
                Condition = condition;
                Type = type;
            }

            public Func<bool> Condition { get; private set; }
            public ControlledMoveToType Type { get; private set; }
        }

        #endregion

        #region Embedded Type - NoMover

        public class NoMover : IPlayerMover
        {
            #region IPlayerMover Members

            public void Move(WoWMovement.MovementDirection direction) { }
            public void MoveStop() { }
            public void MoveTowards(WoWPoint location) { }

            #endregion
        }

        #endregion

        #region Embedded Type - AngleSpan

        public struct AngleSpan
        {
            public const int FrontAngle = 0;
            public const int BehindAngle = 180;
            public const int LeftSideAngle = 90;
            public const int RightSideAngle = 270;

            /// <summary>
            ///     Direction in Degrees
            /// </summary>
            public readonly int Angle;

            /// <summary>
            ///     The Span
            /// </summary>
            public readonly int Span;

            /// <summary>
            /// </summary>
            /// <param name="angle"></param>
            /// <param name="span"></param>
            public AngleSpan(int angle, int span)
            {
                Angle = angle;
                Span = span;
            }
        }

        #endregion

        /*
        #region Embedded Type - UnitLocationFrame
        // this was going to be used for an algorithm that predicts unit movements but ended up not needing it.
        internal class UnitLocationFrame
        {
            public UnitLocationFrame(WoWUnit unit)
            {
                Unit = unit;
                Location = unit.Location;
                TimeStamp = DateTime.Now;
            }

            public DateTime TimeStamp { get; private set; }
            public WoWPoint Location { get; private set; }

            public WoWUnit Unit { get; private set; }

            public void Update()
            {
                if (Unit.IsValid)
                {
                    TimeStamp = DateTime.Now;
                    Location = Unit.Location;
                }
            }
        }

        #endregion
*/
    }
}
