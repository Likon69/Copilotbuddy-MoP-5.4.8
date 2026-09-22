using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Buddy.Coroutines;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.Pathing;
using Styx.Resources;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Styx.WoWInternals.World;
using Tripper.MeshMisc;
using Tripper.Navigation;
using Tripper.RecastManaged.Detour;
using Tripper.Tools.Math;

namespace Styx.CommonBot.Coroutines
{
    public static class CommonCoroutines
    {
        private static readonly WaitTimer _moveToLogTimer = WaitTimer.OneSecond;

        private static WoWPoint _lastMoveToDestination;

        public static Task SleepForLagDuration()
        {
            return Coroutine.Sleep((int)(StyxWoW.WoWClient.Latency + 100));
        }

        public static Task<bool> WaitForLuaEvent(string eventName, int maxWaitMs, Func<bool> alternateCondition = null)
        {
            return WaitForLuaEvent(eventName, TimeSpan.FromMilliseconds(maxWaitMs), alternateCondition);
        }

        [DebuggerStepThrough]
        public static async Task<bool> WaitForLuaEvent(string eventName, TimeSpan maxWaitTimeout, Func<bool> alternateCondition = null)
        {
            bool eventFired = false;
            LuaEventHandlerDelegate handler = (sender, e) => eventFired = true;
            try
            {
                Lua.Events.AttachEvent(eventName, handler);
                return await Coroutine.Wait(maxWaitTimeout, () => eventFired || (alternateCondition != null && alternateCondition()));
            }
            finally
            {
                Lua.Events.DetachEvent(eventName, handler);
            }
        }

        [DebuggerStepThrough]
        public static async Task<bool> LandAndDismount(string reason = "", bool dismount = true, WoWPoint? landPoint = null)
        {
            WoWUnit activeMover = WoWMovement.ActiveMover;
            if (activeMover == null)
            {
                return false;
            }

            if (!(StyxWoW.Me.Mounted && (dismount || activeMover.IsFlying)))
            {
                return false;
            }

            string reasonSuffix = !string.IsNullOrEmpty(reason) ? " Reason: " + reason : string.Empty;
            Logging.WriteDiagnostic(StyxResources.Stop_and_dismount___ + reasonSuffix);
            Stopwatch stopwatch = Stopwatch.StartNew();
            bool forceLanding = false;

            while (!forceLanding)
            {
                if (stopwatch.ElapsedMilliseconds >= 15000L)
                {
                    forceLanding = true;
                    break;
                }

                WoWPoint currentPos = activeMover.Location;
                bool isFlying = activeMover.IsFlying;

                if (isFlying && (!landPoint.HasValue || currentPos.DistanceSqr(landPoint.Value) >= 16f))
                {
                    if (landPoint.HasValue)
                    {
                        if (activeMover.IsFlying)
                        {
                            Flightor.MoveTo(landPoint.Value);
                        }

                        await Coroutine.Yield();
                        continue;
                    }

                    using (StyxWoW.Memory.AcquireFrame())
                    {
                        if (dismount && GameWorld.TraceLine(currentPos, currentPos.Add(0f, 0f, -100f), TraceLineHitFlags.LiquidWaterWalkable | TraceLineHitFlags.LiquidAll, out WoWPoint waterHit))
                        {
                            bool groundHit = GameWorld.TraceLine(currentPos, currentPos.Add(0f, 0f, -110f), TraceLineHitFlags.Collision, out WoWPoint groundPoint);
                            if (!groundHit || waterHit.Z - groundPoint.Z > 5f)
                            {
                                Logging.WriteDiagnostic("Dismounting in air above water.");
                                return await Dismount(reason, descend: false);
                            }
                        }

                        List<float> heights = Navigator.FindHeights(currentPos.X, currentPos.Y);
                        WoWPoint underneathSpot = heights
                            .Select(h => new WoWPoint(currentPos.X, currentPos.Y, h))
                            .OrderBy(p => Math.Abs(p.Z - currentPos.Z))
                            .FirstOrDefault(p => GameWorld.TraceLine(p.Add(0f, 0f, 2f), p.Add(0f, 0f, -2f), TraceLineHitFlags.WmoCollision | TraceLineHitFlags.Terrain));

                        List<WoWObject> nearbyObjects = ObjectManager.EnumerateObjectsOfType<WoWObject>(allowInheritance: true)
                            .OrderBy(o => currentPos.DistanceSqr(o.Location))
                            .ToList();
                        List<WoWPoint> nearbyLocations = nearbyObjects.Select(o => o.Location).ToList();

                        if (!(underneathSpot == WoWPoint.Zero)
                            && nearbyLocations.Any(to => Navigator.CanNavigateWithin(underneathSpot, to, 4f))
                            && !GameWorld.TraceLine(currentPos, underneathSpot, TraceLineHitFlags.DoodadCollision | TraceLineHitFlags.EntityCollision))
                        {
                            Logging.WriteDiagnostic("Landing at the area directly beneath current postion.");
                            forceLanding = true;
                        }
                        else
                        {
                            for (int searchRadius = 10; searchRadius < 250; searchRadius += 10)
                            {
                                TimeSpan remaining = TimeSpan.FromSeconds(15.0) - stopwatch.Elapsed;
                                if (remaining.TotalMilliseconds <= 0.0)
                                {
                                    break;
                                }

                                landPoint = FindSafeLandingSpot(currentPos, searchRadius, searchRadius - 10, nearbyLocations, remaining);
                                if (landPoint.HasValue)
                                {
                                    Logging.WriteDiagnostic("Landing at a safe location: {0} from: {1}", landPoint.Value, currentPos);
                                    break;
                                }
                            }

                            if (!landPoint.HasValue)
                            {
                                Logging.WriteDiagnostic("Could not find a suitable landing spot. Forcing a landing at current position and hoping for the best.");
                                forceLanding = true;
                            }
                        }
                    }

                    if (forceLanding)
                    {
                        break;
                    }

                    if (activeMover.IsFlying)
                    {
                        Flightor.MoveTo(landPoint.Value);
                    }

                    await Coroutine.Yield();
                    continue;
                }

                if (isFlying)
                {
                    Navigator.PlayerMover.MoveTowards(landPoint.Value);
                    await Coroutine.Wait(2000, () => !StyxWoW.Me.IsMoving);
                }

                if (dismount)
                {
                    await Dismount(reason);
                }

                return true;
            }

            if (stopwatch.ElapsedMilliseconds >= 15000L)
            {
                Logging.WriteDiagnostic("Failed to land within 15 seconds. Forcing a landing");
            }

            try
            {
                WoWMovement.Move(WoWMovement.MovementDirection.Descend);
                await Coroutine.Sleep(150);
                bool landed = await Coroutine.Wait(40000, () => !StyxWoW.Me.IsFlying);
                if (!landed)
                {
                    Logging.WriteDiagnostic("Unable to land after 40 seconds of descending.");
                }
            }
            finally
            {
                WoWMovement.MoveStop(WoWMovement.MovementDirection.Descend);
            }

            if (!dismount)
            {
                return true;
            }

            await Dismount(reason);
            return true;
        }

        [DebuggerStepThrough]
        public static async Task<bool> Dismount(string reason = null, bool descend = true)
        {
            if (!StyxWoW.Me.Mounted)
            {
                return false;
            }

            string reasonSuffix = !string.IsNullOrEmpty(reason) ? " Reason: " + reason : string.Empty;
            Logging.WriteDiagnostic(StyxResources.Stop_and_dismount___ + reasonSuffix);
            await StopMoving("Dismounting");

            if (descend && StyxWoW.Me.IsFlying)
            {
                try
                {
                    WoWMovement.Move(WoWMovement.MovementDirection.Descend);
                    await Coroutine.Sleep(150);
                    bool landed = await Coroutine.Wait(40000, () => !StyxWoW.Me.IsFlying);
                    if (!landed)
                    {
                        Logging.WriteDiagnostic("Unable to land after 40 seconds of descending.");
                    }
                }
                finally
                {
                    WoWMovement.MoveStop(WoWMovement.MovementDirection.Descend);
                }
            }

            ShapeshiftForm shapeshift = StyxWoW.Me.Shapeshift;
            if (shapeshift != ShapeshiftForm.FlightForm && shapeshift != ShapeshiftForm.EpicFlightForm)
            {
                Lua.DoString("Dismount()");
            }
            else
            {
                Lua.DoString("RunMacroText('/cancelform')");
            }

            bool stoppedMounting = await Coroutine.Wait(4000, () => !StyxWoW.Me.Mounted);
            if (!stoppedMounting)
            {
                return false;
            }

            Mount.RaiseOnDismount(reason);
            return true;
        }

        // 5.3 has no counterpart for this landing-spot search; it was introduced with
        // the async coroutine layer in 5.4.8 (decompiled name: smethod_0).
        // BLOCKED: relies on Tripper.RecastManaged.Detour.NavMeshQuery.ClosestPointOnPoly,
        // which this project's NavMeshQuery.cs (Tripper\RecastManaged\Detour\NavMeshQuery.cs)
        // deliberately does not implement -- its header states only FindNearestPolygon,
        // QueryPolygons and Raycast are ported ("the rest of HB's ~20-method surface is
        // intentionally not reproduced"). Ported verbatim below anyway per the fidelity
        // rule; it will not compile until ClosestPointOnPoly is added to NavMeshQuery.
        private static WoWPoint? FindSafeLandingSpot(WoWPoint origin, float searchRadius2D, float excludeRadius2D, List<WoWPoint> nearbyLocations, TimeSpan timeout)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            float maxZ = origin.Z + 5f;
            if (excludeRadius2D >= searchRadius2D)
            {
                throw new ArgumentException("excludeRadius2D must be smaller than searchRadius2D", "excludeRadius2D");
            }

            if (!(Navigator.NavigationProvider is MeshNavigator meshNavigator))
            {
                return null;
            }

            Tripper.Tools.Math.Vector3 extents = new Tripper.Tools.Math.Vector3(searchRadius2D, 50000f, searchRadius2D);
            List<WoWPoint> candidates;
            lock (meshNavigator.Nav.MeshLock)
            {
                meshNavigator.Nav.LoadTile(TileIdentifier.GetByPosition(origin.X, origin.Y));
                for (float i = 1f; i <= 8f; i++)
                {
                    float headingRadians = (float)Math.PI * 2f * (i / 8f);
                    meshNavigator.Nav.LoadTile(TileIdentifier.GetByPosition(origin.RayCast(headingRadians, searchRadius2D)));
                }

                Tripper.Tools.Math.Vector3 center = NavHelper.ToNav(new Tripper.Tools.Math.Vector3(origin.X, origin.Y, 0f));
                PolygonReference[] excludedPolys = null;
                if (!meshNavigator.Nav.MeshQuery.QueryPolygons(center, extents, QueryFilter.Default, 1000, out PolygonReference[] allPolys).Succeeded)
                {
                    return null;
                }

                if (excludeRadius2D > 1E-05)
                {
                    extents = new Tripper.Tools.Math.Vector3(excludeRadius2D, 50000f, excludeRadius2D);
                    if (!meshNavigator.Nav.MeshQuery.QueryPolygons(center, extents, QueryFilter.Default, 1000, out excludedPolys).Succeeded)
                    {
                        return null;
                    }
                }

                candidates = new List<WoWPoint>();
                PolygonReference[] polys = allPolys;
                foreach (PolygonReference poly in polys)
                {
                    if ((excludedPolys == null || !excludedPolys.Any(excluded => excluded.Id == poly.Id))
                        && meshNavigator.Nav.MeshQuery.ClosestPointOnPoly(poly, center, out Tripper.Tools.Math.Vector3 closest).Succeeded)
                    {
                        WoWPoint closestWow = NavHelper.ToWow(closest);
                        if (!(closestWow.Z > maxZ))
                        {
                            candidates.Add(closestWow);
                        }
                    }
                }
            }

            GameWorld.MassTraceLine(candidates.Select(p => new WorldLine(p.Add(0f, 0f, 2f), p.Add(0f, 0f, -2f))).ToArray(), TraceLineHitFlags.WmoCollision | TraceLineHitFlags.Terrain, out bool[] hitResults);
            candidates = candidates.Where((p, i) => hitResults[i]).OrderBy(origin.DistanceSqr).ToList();

            GameWorld.MassTraceLine(candidates.Select(p => new WorldLine(p.Add(0f, 0f, maxZ - p.Z), p.Add(0f, 0f, 2f))).ToArray(), TraceLineHitFlags.Collision, out hitResults);

            for (int i = 0; i < hitResults.Length; i++)
            {
                if (!hitResults[i])
                {
                    WoWPoint candidate = candidates[i];
                    if (nearbyLocations.Any(to => Navigator.CanNavigateWithin(candidate, to, 4f)))
                    {
                        if (stopwatch.Elapsed > timeout)
                        {
                            return null;
                        }

                        return candidate;
                    }
                }
            }

            return null;
        }

        [DebuggerStepThrough]
        public static async Task<bool> StopMoving(string reason = null)
        {
            WoWUnit activeMover = WoWMovement.ActiveMover;
            if (activeMover == null || !activeMover.IsMoving)
            {
                return false;
            }

            WoWMovement.MoveStop();
            string reasonSuffix = !string.IsNullOrEmpty(reason) ? " Reason: " + reason : string.Empty;
            Logging.WriteDiagnostic("Stopped moving." + reasonSuffix);

            bool stopped = await Coroutine.Wait(4000, () => !activeMover.IsMoving);
            if (!stopped)
            {
                Logging.WriteDiagnostic("Unable to stop moving after 4 seconds of attempting to stop");
            }

            return true;
        }

        [DebuggerStepThrough]
        public static async Task<MoveResult> MoveTo(WoWPoint destination, string destinationName = null)
        {
            if (destination.DistanceSqr(_lastMoveToDestination) > 25f && _moveToLogTimer.IsFinished)
            {
                destinationName = destinationName ?? destination.ToString();
                Logging.WriteDiagnostic("Moving to {0}", destinationName);
                _moveToLogTimer.Reset();
                _lastMoveToDestination = destination;
            }

            return Navigator.MoveTo(destination);
        }
    }
}
