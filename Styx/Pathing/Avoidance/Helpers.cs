using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Styx.Common;
using Styx.Helpers;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Vector3 = Tripper.Tools.Math.Vector3;

namespace Styx.Pathing.Avoidance
{
    public static class Helpers
    {
        private static MeshNavigator Nav { get { return (MeshNavigator)Navigator.NavigationProvider; } }

        /// <summary>
        ///   Sets to nearest Z.
        /// </summary>
        /// <param name="point"> The point. </param>
        /// <returns> </returns>
        internal static Vector3 SetToNearestZ(Vector3 point)
        {
            try
            {
                List<float> zlist = Navigator.FindHeights(point.X, point.Y);
                if (zlist != null && zlist.Count > 0)
                {
                    point.Z = zlist.OrderBy(z => Math.Abs(z - point.Z)).FirstOrDefault();
                }
            }
            // ReSharper disable EmptyGeneralCatchClause
            catch
            {
            }
            // ReSharper restore EmptyGeneralCatchClause
            return point;
        }

        public static float Distance2DSqr(this Vector3 from, Vector3 to)
        {
            float num = from.X - to.X;
            float num2 = from.Y - to.Y;
            return num * num + num2 * num2;
        }

        private static readonly Dictionary<Avoid, WoWPoint> PrevAvoids = new Dictionary<Avoid, WoWPoint>();
        private static AvoidPathResult _currentAvoidPath;
        private static WoWPoint _prevDestination;
        private const float PIx2 = (float)Math.PI * 2;
        internal const float Padding = 0.5f;
        internal const float PaddingSqr = Padding * Padding;
        internal const float SubPadding = Padding / 2;
        internal const int MaxRecursionLevel = 80;

        /// <summary>
        /// Returns a path around avoids or null if none is needed
        /// </summary>
        /// <param name="destination"></param>
        /// <returns></returns>
        public static AvoidPathResult GetAvoidPath(WoWPoint destination)
        {
            WoWPoint myLoc = StyxWoW.Me.Location;
            AvoidCluster clusterAtFrom = MeshAvoidanceManager.AvoidClusters.FirstOrDefault(c => c.Any(a => a.IsPointInAvoid(myLoc)));

            // if we're standing in an avoidcluster then we neeed to get out.
            if (clusterAtFrom != null)
            {
                Vector3 moveTo = GetNearestLocationOutsideCluster(StyxWoW.Me.Location, clusterAtFrom, Navigator.PathPrecision + Padding);
                if (!moveTo.Equals(Vector3.Zero))
                {
                    // return to caller with 'get out of avoidcluster' path
                    return new AvoidPathResult(PathResult.Changed, Nav.GeneratePath(myLoc, moveTo).Select(p => (Vector3)p).ToArray());
                }
            }

            List<Avoid> activeAvoids = MeshAvoidanceManager.Avoids;

            // check if anything has changed since last visit.
            if (PrevAvoids.Any() && PrevAvoids.Count == activeAvoids.Count()
                && activeAvoids.All(a => PrevAvoids.ContainsKey(a) && PrevAvoids.ContainsValue(a.Location))
                && _prevDestination.DistanceSqr(destination) < PaddingSqr)
            {
                return activeAvoids.Any() ? _currentAvoidPath : null;
            }

            if (Nav.CurrentMovePath == null)
            {
                return null;
            }

            WoWPoint originalDestination = destination;

            AvoidCluster clusterAtEnd = MeshAvoidanceManager.AvoidClusters.FirstOrDefault(c => c.Any(a => a.IsPointInAvoid(destination)));
            if (clusterAtEnd != null)
            {
                destination = GetNearestLocationOutsideCluster(destination, clusterAtEnd, Navigator.PathPrecision + Padding);
                if (destination == WoWPoint.Zero)
                {
                    return null;
                }
            }
            // check if we need to regenerate the navigation path...
            if (activeAvoids.Count != PrevAvoids.Count && _prevDestination == originalDestination)
            {
                Nav.CurrentMovePath.Path = Nav.FindPath(myLoc, destination);
                Nav.CurrentMovePath.Index = 0;
            }

            PrevAvoids.Clear();
            foreach (Avoid avoid in activeAvoids)
            {
                PrevAvoids.Add(avoid, avoid.Location);
            }

            if (!activeAvoids.Any())
            {
                return null;
            }

            Vector3[] currentNavigationPath = Nav.CurrentMovePath.Path.Points.Skip(Nav.CurrentMovePath.Index).ToArray();
            AvoidPathResult newPath;
            try
            {
                newPath = GenPath(myLoc, destination, currentNavigationPath, 0);
            }
            catch (AvoidPathNotFoundException ex)
            {
                Logging.WriteDiagnostic("Avoid path generation has failed: {0}", ex.Message);
                // dump some debug info
                Logging.WriteDiagnostic("Start:{0}, End:{1}", myLoc, destination);
                foreach (Avoid activeAvoid in activeAvoids)
                {
                    Logging.WriteDiagnostic("\tLocation:{0}, Radius {1} ", activeAvoid.Location, activeAvoid.Radius);
                }
                ClearAvoidPath();
                return null;
            }

            _prevDestination = originalDestination;
            if (newPath.Result == PathResult.Unchanged || newPath.Result == PathResult.Failed)
            {
                return null;
            }

            _currentAvoidPath = newPath;
            return _currentAvoidPath;
        }

        public static void ClearAvoidPath()
        {
            PrevAvoids.Clear();
            _currentAvoidPath = null;
        }

        // returns the avoid path
        private static AvoidPathResult GenPath(Vector3 start, Vector3 end, Vector3[] navigatorPath, int recursionLevel)
        {
            for (int pathI = 0; pathI < navigatorPath.Length; pathI++)
            {
                Vector3 from = pathI - 1 < 0 ? start : navigatorPath[pathI - 1];
                Vector3 to = navigatorPath[pathI];
                ClusterHit clusterHit = AvoidClusterTraceline(MeshAvoidanceManager.AvoidClusters, from, to);

                if (clusterHit != null)
                {
                    LineClusterTangentPoints tangents = clusterHit.Cluster.GetLineTangentPoints(from);
                    if (tangents != null)
                    {
                        // find best side and try to genorate paths for it.
                        AvoidSide bestSide = BestClusterSide(clusterHit.Point, tangents.RightPoint, tangents.LeftPoint, from);

                        var leftSide = new { Avoid = tangents.LeftAvoid, Point = tangents.LeftPoint, Side = AvoidSide.Left };
                        var rightSide = new { Avoid = tangents.RightAvoid, Point = tangents.RightPoint, Side = AvoidSide.Right };

                        var bestSideChoice = bestSide == AvoidSide.Right ? rightSide : leftSide;
                        var worstSideChoice = bestSide == AvoidSide.Right ? leftSide : rightSide;

                        AvoidPathResult returnVal1 = ProcessBranch(clusterHit.Cluster, bestSideChoice.Avoid, bestSideChoice.Point, bestSideChoice.Side, start, end, recursionLevel);
                        // if the best side fails then try other side.
                        if (returnVal1.Result <= PathResult.Partial)
                        {
                            AvoidPathResult returnVal2 = ProcessBranch(
                                clusterHit.Cluster,
                                worstSideChoice.Avoid,
                                worstSideChoice.Point,
                                worstSideChoice.Side,
                                start,
                                end,
                                recursionLevel);

                            if (returnVal2.Result > PathResult.Partial || (returnVal1.Result == PathResult.Failed && returnVal2.Result == PathResult.Partial))
                            {
                                return returnVal2;
                            }
                            // if both branches fail then we return point of interception.
                            if (returnVal1.Result == PathResult.Failed && returnVal2.Result == PathResult.Failed)
                            {
                                List<Vector3> path = navigatorPath.Take(pathI).ToList();
                                path.Add(WoWMathHelper.CalculatePointFrom(start, clusterHit.Point, Navigator.PathPrecision + SubPadding));
                                returnVal1 = new AvoidPathResult(PathResult.Partial, path.ToArray());
                            }
                        }
                        return returnVal1;
                    }
                }
            }
            return new AvoidPathResult(PathResult.Unchanged, navigatorPath);
        }

        private static AvoidPathResult ProcessBranch(AvoidCluster cluster, Avoid avoidHit, Vector3 tangentPoint, AvoidSide avoidSide, Vector3 start, Vector3 end, int recursionLevel)
        {
            if (recursionLevel >= MaxRecursionLevel)
            {
                throw new AvoidPathNotFoundException(string.Format("Recursion limit was reached ({0})", MaxRecursionLevel));
            }

            var newPath = new List<Vector3>();

            PathResult returnVal = PathResult.Changed;

            AvoidPathResult endPathResult1 = null;

            Vector3 relativePoint = tangentPoint - (Vector3)avoidHit.Location;
            var startTangentAngle = (float)Math.Atan2(relativePoint.Y, relativePoint.X);

            Vector3 circleStartPoint = SetToNearestZ(avoidHit.Location.RayCast(startTangentAngle, avoidHit.Radius + Padding));
            Vector3 previousCirclePoint = Vector3.Zero;

            Vector3 circlePoint = circleStartPoint;
            // if circle points is very close to end then we stop.
            if (circlePoint.Distance2DSqr(end) < SubPadding)
            {
                return new AvoidPathResult(PathResult.Changed, new[] { start, end });
            }

            AvoidPathResult ret1 = GeneratePathSegment(start, circlePoint, avoidHit, recursionLevel);
            if (ret1.Result <= PathResult.Partial)
            {
                return ret1;
            }
            newPath.AddRange(ret1.Path.Take(ret1.Path.Length - 1));

            var circleSegment = new List<Vector3>();
            bool tryWorstPath = false;
            const float stepSize = 0.05f;
            float step = stepSize;
            int iterations = 0;
            do
            {
                // check if point is navigatable. we only need one hop.
                if (!previousCirclePoint.Equals(Vector3.Zero) && !Navigator.CanNavigateFully(previousCirclePoint, circlePoint)
                    || iterations++ > 100) // temp hackisk fix tile find the real cause of inifite loops
                {
                    returnVal = PathResult.Failed;
                    break;
                }
                // check if an avoid in current cluster is blocking path
                AvoidTracelineResult[] traces = GetAvoidsHitByTraceline(cluster, circlePoint, end).OrderBy(t => t.Enter.DistanceSqr(circlePoint) - t.Avoid.RadiusSqr).ToArray();

                // end is in sight! we now just genorate path to it.
                if (traces.Length == 0)
                {
                    AvoidPathResult ret = GeneratePathSegment(circlePoint, end, avoidHit, recursionLevel);
                    newPath.AddRange(circleSegment);
                    newPath.AddRange(ret.Path);
                    returnVal = ret.Result;
                    break;
                }
                // we hit another avoid on same cluster..
                if (traces[0].Avoid != avoidHit)
                {
                    LineCircleTangentPoints tang = null;
                    bool obsticleBetweenCirclepointAndTangent = false;
                    //  lets back up some
                    for (int i = 0; i < circleSegment.Count; i++)
                    {
                        tang = GetLineCircleTangentPoints(traces[0].Avoid.Location, traces[0].Avoid.Radius + Padding, circleSegment[i]);
                        if (tang != null)
                        {
                            Vector3 tangPoint = avoidSide == AvoidSide.Right ? tang.RightPoint : tang.LeftPoint;
                            obsticleBetweenCirclepointAndTangent = GetAvoidsHitByTraceline(MeshAvoidanceManager.Avoids, circleSegment[i], tangPoint).Any();
                            if (!obsticleBetweenCirclepointAndTangent)
                            {
                                if (i + 1 < circleSegment.Count)
                                {
                                    circleSegment.RemoveRange(i + 1, circleSegment.Count - (i + 1));
                                }
                                circlePoint = circleSegment[i];
                                break;
                            }
                        }
                    }
                    // ToDo.. take shorter steps in attempt to increase the success rate of getting around adjacent avoids.
                    if (tang == null || obsticleBetweenCirclepointAndTangent)
                    {
                        if (circleSegment.Count > 0)
                        {
                            circleSegment.RemoveAt(circleSegment.Count - 1);
                        }
                        newPath.AddRange(circleSegment);
                        returnVal = PathResult.Partial;
                        break;
                    }
                    avoidHit = traces[0].Avoid;
                    relativePoint = (avoidSide == AvoidSide.Right ? tang.RightPoint : tang.LeftPoint) - (Vector3)avoidHit.Location;
                    startTangentAngle = (float)Math.Atan2(relativePoint.Y, relativePoint.X);

                    WoWPoint newCircleStartPoint = avoidHit.Location.RayCast(startTangentAngle, avoidHit.Radius + Padding);

                    AvoidPathResult ret = GeneratePathSegment(circlePoint, newCircleStartPoint, avoidHit, recursionLevel);

                    circleSegment.AddRange(ret.Path);
                    if (ret.Result <= PathResult.Partial)
                    {
                        newPath.AddRange(circleSegment);
                        returnVal = ret.Result;
                        break;
                    }

                    step = stepSize;
                    circlePoint = newCircleStartPoint;
                    continue;
                }

                ClusterHit clusterHit = AvoidClusterTraceline(MeshAvoidanceManager.AvoidClusters.Where(c => c != cluster).ToList(), circlePoint, end);
                LineClusterTangentPoints tangents = clusterHit != null ? clusterHit.Cluster.GetLineTangentPoints(circlePoint) : null;

                // check if we have another cluster blocking path to end.
                if (tangents != null)
                {
                    AvoidSide bestSide = BestClusterSide(clusterHit.Point, tangents.RightPoint, tangents.LeftPoint, start);

                    var leftSide = new { Avoid = tangents.LeftAvoid, Point = tangents.LeftPoint, Side = AvoidSide.Left };
                    var rightSide = new { Avoid = tangents.RightAvoid, Point = tangents.RightPoint, Side = AvoidSide.Right };

                    var bestSideChoice = bestSide == AvoidSide.Right ? rightSide : leftSide;
                    var worstSideChoice = bestSide == AvoidSide.Right ? leftSide : rightSide;

                    var selectedSide = !tryWorstPath ? bestSideChoice : worstSideChoice;

                    // check if we can move to the tangent of cluster blocking our way.
                    if (AvoidTraceline(avoidHit, circlePoint, selectedSide.Point).Hits == 0)
                    {
                        if (!tryWorstPath)
                        {
                            endPathResult1 = ProcessBranch(clusterHit.Cluster, selectedSide.Avoid, selectedSide.Point, selectedSide.Side, circlePoint, end, recursionLevel);
                            // check if other side has better results
                            if (endPathResult1.Result <= PathResult.Partial)
                            {
                                step = stepSize;
                                tryWorstPath = true;
                                continue;
                            }
                            newPath.AddRange(circleSegment);
                            newPath.AddRange(endPathResult1.Path);
                            break;
                        }
                        AvoidPathResult endPathResult2 = ProcessBranch(clusterHit.Cluster, selectedSide.Avoid, selectedSide.Point, selectedSide.Side, circlePoint, end, recursionLevel);
                        newPath.AddRange(circleSegment);
                        newPath.AddRange(endPathResult1.Result >= endPathResult2.Result ? endPathResult1.Path : endPathResult2.Path);
                        returnVal = endPathResult1.Result >= endPathResult2.Result ? endPathResult1.Result : endPathResult2.Result;
                        break;
                    }
                }

                circleSegment.Add(circlePoint);

                float relativeAngle = WoWMathHelper.NormalizeRadian(PIx2 * step);
                float ang = (avoidSide != AvoidSide.Right ? -relativeAngle : relativeAngle) + startTangentAngle;
                previousCirclePoint = circlePoint;
                circlePoint = SetToNearestZ(avoidHit.Location.RayCast(ang, avoidHit.Radius + Padding));
                step += stepSize;

            } while (step < 1);
            return new AvoidPathResult(returnVal > PathResult.Partial ? PathResult.Changed : returnVal, newPath.ToArray());
        }

        private static AvoidPathResult GeneratePathSegment(Vector3 start, Vector3 end, Avoid currentAvoid, int recursionLevel)
        {
            Tripper.Navigation.PathFindResult pathResult = Nav.FindPath(start, end);
            // if point any of the points generated in in the current avoid then it has failed.
            if (pathResult.Points.Any(p => currentAvoid != null && currentAvoid.IsPointInAvoid(p)) || !pathResult.Succeeded)
            {
                return new AvoidPathResult(PathResult.Failed, pathResult.Points);
            }

            if (pathResult.IsPartialPath)
            {
                return new AvoidPathResult(PathResult.Partial, pathResult.Points);
            }

            return GenPath(start, end, pathResult.Points, recursionLevel + 1);
        }

        public static Vector3 GetNearestLocationOutsideCluster(Vector3 location, AvoidCluster cluster, float extendDistance)
        {
            var exitLocations = new List<Vector3>();
            for (float step = 0; step < 1; step += 0.05f)
            {
                float ang = PIx2 * step;
                Vector3 loc = GetAvoidsHitByTraceline(cluster, location, WoWMathHelper.GetPointAt(location, 100, ang, 0))
                    .Where(t => t.Avoid.LeashPoint == WoWPoint.Zero || t.Avoid.LeashPoint.DistanceSqr(t.Exit) < t.Avoid.LeashRadiusSqr)
                    .OrderByDescending(t => location.Distance2DSqr(t.Exit))
                    .Select(t => t.Exit)
                    .FirstOrDefault();

                if (!loc.Equals(Vector3.Zero))
                {
                    exitLocations.Add(loc);
                }
            }
            foreach (Vector3 exitLocation in exitLocations.OrderBy(loc => location.Distance2DSqr(loc)))
            {
                Vector3 nearestOutsideLoc = SetToNearestZ(WoWMathHelper.CalculatePointFrom(location, exitLocation, -extendDistance)); // extend point pass avoid radius
                if (Navigator.CanNavigateFully(location, nearestOutsideLoc))
                {
                    return nearestOutsideLoc;
                }
            }
            // shouldn't get to here in most cases.
            return Vector3.Zero;
        }

        internal static MoveResult MoveOutOfAvoidCluster(AvoidCluster cluster)
        {
            Vector3 moveTo = GetNearestLocationOutsideCluster(StyxWoW.Me.Location, cluster, Navigator.PathPrecision + Padding);
            if (moveTo.Equals(Vector3.Zero))
            {
                return MoveResult.Failed;
            }

            ClearAvoidPath();

            if (cluster[0] is AvoidObject)
            {
                string name = ((AvoidObject)cluster[0]).Object is WoWDynamicObject
                    ? WoWSpell.FromId((int)((AvoidObject)cluster[0]).Object.Entry).Name
                    : ((AvoidObject)cluster[0]).Object.Name;

                Logging.Write("Running from {0}", name);
            }
            else if (cluster[0] is AvoidLocation)
            {
                Logging.Write("Running from location {0}", cluster[0].Location);
            }
            if (StyxWoW.Me.Location.DistanceSqr(moveTo) <=
                Navigator.PathPrecision * Navigator.PathPrecision)
            {
                WoWMovement.ClickToMove(moveTo);
                return MoveResult.Moved;
            }
            return Navigator.MoveTo(moveTo);
        }

        private static AvoidSide BestClusterSide(Vector3 clusterHitPoint, Vector3 rightPoint, Vector3 leftPoint, Vector3 externalPoint)
        {
            var hitPointAngle = (float)Math.Atan2(clusterHitPoint.Y - externalPoint.Y, clusterHitPoint.X - externalPoint.X);
            var rightAngle = (float)Math.Atan2(rightPoint.Y - externalPoint.Y, rightPoint.X - externalPoint.X);
            var leftAngle = (float)Math.Atan2(leftPoint.Y - externalPoint.Y, leftPoint.X - externalPoint.X);

            return WoWMathHelper.NormalizeRadian(leftAngle - hitPointAngle) > WoWMathHelper.NormalizeRadian(hitPointAngle - rightAngle) ? AvoidSide.Right : AvoidSide.Left;
        }

        /// <summary>
        ///   Returns a list of avoids from the list passed as a parameter that are hit by traceline in the ascending order by which they were hit 1st.
        /// </summary>
        /// <param name="start"> The start. </param>
        /// <param name="end"> The end. </param>
        /// <param name="avoids"> The avoids. </param>
        /// <returns> </returns>
        public static IEnumerable<AvoidTracelineResult> GetAvoidsHitByTraceline(IEnumerable<Avoid> avoids, Vector3 start, Vector3 end)
        {
            return from avoid in avoids
                   let trace = AvoidTraceline(avoid, start, end)
                   where trace.Hits > 0
                   // note: using distance check from end location since if there is an avoid inside start it can produce inaccurate results.
                   orderby end.Distance2DSqr(trace.Enter) descending
                   select trace;
        }

        public static ClusterHit AvoidClusterTraceline(List<AvoidCluster> avoidClusters, Vector3 start, Vector3 end)
        {
            return (from cluster in avoidClusters
                    let nearestHit = GetAvoidsHitByTraceline(cluster, start, end).OrderBy(t => start.DistanceSqr(t.Enter)).FirstOrDefault()
                    where nearestHit != null
                    let relativePoint = nearestHit.Exit - nearestHit.Enter
                    orderby start.DistanceSqr(nearestHit.Enter)
                    select new ClusterHit(cluster, nearestHit.Enter, (float)Math.Atan2(relativePoint.Y, relativePoint.X))).FirstOrDefault();
        }

        public static void BuildClusters(List<Avoid> avoids, List<AvoidCluster> clusters)
        {
            clusters.Clear();
            foreach (Avoid outerAvoid in avoids)
            {
                if (clusters.Any(c => c.Contains(outerAvoid)))
                {
                    continue;
                }
                var avoidCluster = new AvoidCluster(outerAvoid);
                while (true)
                {
                    Avoid match = avoids.Where(a => !avoidCluster.Contains(a)).FirstOrDefault(
                        a => avoidCluster.Any(
                            clusteredAvoid =>
                            {
                                float minDist = clusteredAvoid.Radius + a.Radius + Padding * 2;
                                float distSqr = clusteredAvoid.Location.DistanceSqr(a.Location);
                                return distSqr < minDist * minDist;
                            }));
                    if (match != null)
                    {
                        avoidCluster.Add(match);
                    }
                    else
                    {
                        break;
                    }
                }
                clusters.Add(avoidCluster);
            }
        }

        public static AvoidTracelineResult AvoidTraceline(Avoid avoid, Vector3 start, Vector3 end)
        {
            Vector3 enter, exit;
            int hits = GetLineCircleIntersections(avoid.Location, avoid.Radius + SubPadding, start, end, out enter, out exit);
            return new AvoidTracelineResult(hits, enter, exit, avoid);
        }

        public static bool LineAvoidCollision(Avoid avoid, Vector3 start, Vector3 end)
        {
            Vector3 enter, exit;
            return GetLineCircleIntersections(avoid.Location, avoid.Radius + SubPadding, start, end, out enter, out exit) > 0;
        }

        /// <summary>
        ///   returns the number of collisions of a line passing through a circle. Note: algorithm counts small lines inside circle as hits.
        /// </summary>
        /// <param name="circlePos"> The circle pos. </param>
        /// <param name="radius"> The radius. </param>
        /// <param name="start"> The start. </param>
        /// <param name="end"> The end. </param>
        /// <param name="enterPoint"> The line enter point. </param>
        /// <param name="exitPoint"> The line exit point. </param>
        /// <returns> </returns>
        public static int GetLineCircleIntersections(Vector3 circlePos, float radius, Vector3 start, Vector3 end, out Vector3 enterPoint, out Vector3 exitPoint)
        {
            float avoidRadiusSqr = radius * radius;
            float t;
            float cx = start.X - circlePos.X;
            float cy = start.Y - circlePos.Y;

            float dx = end.X - start.X;
            float dy = end.Y - start.Y;

            float a = dx * dx + dy * dy; // dy·dy
            float b = 2 * (dx * cx + dy * cy); // 2 * (dy·cy)
            float c = cx * cx + cy * cy - avoidRadiusSqr; // cx·cy - radius²

            float discriminant = b * b - 4 * a * c;
            if ((a <= 0.0000001) || (discriminant < 0))
            {
                // No real solutions.
                enterPoint = new Vector3(float.NaN, float.NaN, 0f);
                exitPoint = new Vector3(float.NaN, float.NaN, 0f);
                return 0;
            }
            // 1 hit
            if (Math.Abs(discriminant - 0) < float.Epsilon)
            {
                // One solution.
                t = -b / (2 * a);
                enterPoint = new Vector3(start.X + t * dx, start.Y + t * dy, circlePos.Z);
                exitPoint = new Vector3(float.NaN, float.NaN, 0f);
                return 1;
            }
            // Two solutions.

            var discriminantRoot = (float)Math.Sqrt(discriminant);
            // enter point
            t = (-b - discriminantRoot) / (2 * a);
            enterPoint = new Vector3(start.X + t * dx, start.Y + t * dy, circlePos.Z);
            // exit point
            t = (-b + discriminantRoot) / (2 * a);
            exitPoint = new Vector3(start.X + t * dx, start.Y + t * dy, circlePos.Z);

            Vector3 testPoint = start.Distance2DSqr(circlePos) < end.Distance2DSqr(circlePos) ? end : start;
            if (testPoint.Distance2DSqr(enterPoint) > a && testPoint.Distance2DSqr(exitPoint) > a && start.Distance2DSqr(circlePos) > avoidRadiusSqr && end.Distance2DSqr(circlePos) > avoidRadiusSqr)
            {
                return 0;
            }

            return 2;
        }

        public static LineCircleTangentPoints GetLineCircleTangentPoints(Vector3 circlePos, float circleRadius, Vector3 externalPoint)
        {
            var relativePoint = new Vector3(externalPoint.X - circlePos.X, externalPoint.Y - circlePos.Y, 0);
            float hypotenuseLengthSqr = (relativePoint.X * relativePoint.X) + (relativePoint.Y * relativePoint.Y);
            float radiusSqr = circleRadius * circleRadius;
            if (hypotenuseLengthSqr >= radiusSqr)
            {
                float ratio = 1.0f / hypotenuseLengthSqr;
                var oppositeLength = (float)Math.Sqrt(Math.Abs(hypotenuseLengthSqr - radiusSqr));

                float x1 = circlePos.X + circleRadius * (circleRadius * relativePoint.X - relativePoint.Y * oppositeLength) * ratio;
                float y1 = circlePos.Y + circleRadius * (circleRadius * relativePoint.Y + relativePoint.X * oppositeLength) * ratio;
                float x2 = circlePos.X + circleRadius * (circleRadius * relativePoint.X + relativePoint.Y * oppositeLength) * ratio;
                float y2 = circlePos.Y + circleRadius * (circleRadius * relativePoint.Y - relativePoint.X * oppositeLength) * ratio;
                var rightPoint = new Vector3(x1, y1, circlePos.Z);
                var leftPoint = new Vector3(x2, y2, circlePos.Z);
                return new LineCircleTangentPoints(rightPoint, leftPoint);
            }
            return null;
        }
    }
}
