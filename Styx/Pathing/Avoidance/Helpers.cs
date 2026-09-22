using System;
using System.Collections.Generic;
using System.Linq;
using Tripper.Tools.Math;

namespace Styx.Pathing.Avoidance
{
    public static class Helpers
    {
        internal const float Padding = 0.5f;
        internal const float PaddingSqr = 0.25f;
        internal const float SubPadding = 0.25f;
        internal const int MaxRecursionLevel = 80;

        public static float Distance2DSqr(this Vector3 from, Vector3 to)
        {
            float x = from.X - to.X;
            float y = from.Y - to.Y;
            return x * x + y * y;
        }

        public static IEnumerable<AvoidTracelineResult> GetAvoidsHitByTraceline(
            IEnumerable<Avoid> avoids, Vector3 start, Vector3 end)
        {
            return from avoid in avoids
                   let trace = AvoidTraceline(avoid, start, end)
                   where trace.Hits > 0
                   orderby end.Distance2DSqr(trace.Enter) descending
                   select trace;
        }

        public static ClusterHit AvoidClusterTraceline(List<AvoidCluster> avoidClusters, Vector3 start, Vector3 end)
        {
            return (from cluster in avoidClusters
                    let nearestHit = GetAvoidsHitByTraceline(cluster, start, end)
                        .OrderBy(trace => start.DistanceSqr(trace.Enter))
                        .FirstOrDefault()
                    where nearestHit != null
                    let relativePoint = nearestHit.Exit - nearestHit.Enter
                    orderby start.DistanceSqr(nearestHit.Enter)
                    select new ClusterHit(cluster, nearestHit.Enter,
                        (float)Math.Atan2(relativePoint.Y, relativePoint.X))).FirstOrDefault();
        }

        public static void BuildClusters(List<Avoid> avoids, List<AvoidCluster> clusters)
        {
            clusters.Clear();
            foreach (Avoid outerAvoid in avoids)
            {
                if (clusters.Any(cluster => cluster.Contains(outerAvoid)))
                    continue;

                var cluster = new AvoidCluster(outerAvoid);
                while (true)
                {
                    Avoid match = avoids.Where(avoid => !cluster.Contains(avoid)).FirstOrDefault(
                        avoid => cluster.Any(clusteredAvoid =>
                        {
                            float minimumDistance = clusteredAvoid.Radius + avoid.Radius + 1f;
                            float distanceSquared = clusteredAvoid.Location.DistanceSqr(avoid.Location);
                            return distanceSquared < minimumDistance * minimumDistance;
                        }));

                    if (match == null)
                        break;

                    cluster.Add(match);
                }

                clusters.Add(cluster);
            }
        }

        public static AvoidTracelineResult AvoidTraceline(Avoid avoid, Vector3 start, Vector3 end)
        {
            Vector3 enter;
            Vector3 exit;
            int hits = GetLineCircleIntersections(avoid.Location, avoid.Radius + 0.25f,
                start, end, out enter, out exit);
            return new AvoidTracelineResult(hits, enter, exit, avoid);
        }

        public static bool LineAvoidCollision(Avoid avoid, Vector3 start, Vector3 end)
        {
            Vector3 enter;
            Vector3 exit;
            return GetLineCircleIntersections(avoid.Location, avoid.Radius + 0.25f,
                start, end, out enter, out exit) > 0;
        }

        public static int GetLineCircleIntersections(Vector3 circlePos, float radius, Vector3 start,
            Vector3 end, out Vector3 enterPoint, out Vector3 exitPoint)
        {
            float radiusSquared = radius * radius;
            float circleX = start.X - circlePos.X;
            float circleY = start.Y - circlePos.Y;
            float deltaX = end.X - start.X;
            float deltaY = end.Y - start.Y;
            float a = deltaX * deltaX + deltaY * deltaY;
            float b = 2f * (deltaX * circleX + deltaY * circleY);
            float c = circleX * circleX + circleY * circleY - radiusSquared;
            float discriminant = b * b - 4f * a * c;

            if (a <= 0.0000001f || discriminant < 0f)
            {
                enterPoint = new Vector3(float.NaN, float.NaN, 0f);
                exitPoint = new Vector3(float.NaN, float.NaN, 0f);
                return 0;
            }

            float t;
            if (Math.Abs(discriminant) < float.Epsilon)
            {
                t = -b / (2f * a);
                enterPoint = new Vector3(start.X + t * deltaX, start.Y + t * deltaY, circlePos.Z);
                exitPoint = new Vector3(float.NaN, float.NaN, 0f);
                return 1;
            }

            float root = (float)Math.Sqrt(discriminant);
            t = (-b - root) / (2f * a);
            enterPoint = new Vector3(start.X + t * deltaX, start.Y + t * deltaY, circlePos.Z);
            t = (-b + root) / (2f * a);
            exitPoint = new Vector3(start.X + t * deltaX, start.Y + t * deltaY, circlePos.Z);

            Vector3 testPoint = start.Distance2DSqr(circlePos) < end.Distance2DSqr(circlePos) ? end : start;
            if (testPoint.Distance2DSqr(enterPoint) > a && testPoint.Distance2DSqr(exitPoint) > a &&
                start.Distance2DSqr(circlePos) > radiusSquared && end.Distance2DSqr(circlePos) > radiusSquared)
                return 0;

            return 2;
        }

        public static LineCircleTangentPoints GetLineCircleTangentPoints(
            Vector3 circlePos, float circleRadius, Vector3 externalPoint)
        {
            var relativePoint = new Vector3(externalPoint.X - circlePos.X,
                externalPoint.Y - circlePos.Y, 0f);
            float hypotenuseSquared = relativePoint.X * relativePoint.X + relativePoint.Y * relativePoint.Y;
            float radiusSquared = circleRadius * circleRadius;
            if (hypotenuseSquared < radiusSquared)
                return null;

            float ratio = 1f / hypotenuseSquared;
            float oppositeLength = (float)Math.Sqrt(Math.Abs(hypotenuseSquared - radiusSquared));
            float x1 = circlePos.X + circleRadius *
                (circleRadius * relativePoint.X - relativePoint.Y * oppositeLength) * ratio;
            float y1 = circlePos.Y + circleRadius *
                (circleRadius * relativePoint.Y + relativePoint.X * oppositeLength) * ratio;
            float x2 = circlePos.X + circleRadius *
                (circleRadius * relativePoint.X + relativePoint.Y * oppositeLength) * ratio;
            float y2 = circlePos.Y + circleRadius *
                (circleRadius * relativePoint.Y - relativePoint.X * oppositeLength) * ratio;
            return new LineCircleTangentPoints(
                new Vector3(x1, y1, circlePos.Z),
                new Vector3(x2, y2, circlePos.Z));
        }
    }
}
