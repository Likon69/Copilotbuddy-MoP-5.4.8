using System;
using System.Collections.Generic;
using System.Linq;
using Styx.Helpers;
using Tripper.Tools.Math;

namespace Styx.Pathing.Avoidance
{
    public class AvoidCluster : List<Avoid>
    {
        public AvoidCluster(params Avoid[] avoids)
        {
            AddRange(avoids);
        }

        public LineClusterTangentPoints GetLineTangentPoints(Vector3 externalPoint)
        {
            var results = (from avoid in this
                           let tangents = Avoidance.Helpers.GetLineCircleTangentPoints(
                               avoid.Location, avoid.Radius + 0.25f, externalPoint)
                           where tangents != null
                           select new { Avoid = avoid, Tangents = tangents }).ToArray();

            if (results.Length == 0)
                return null;

            Vector3 centerRelative = Center - externalPoint;
            float rootAngle = (float)Math.Atan2(centerRelative.Y, centerRelative.X);

            var left = (from result in results
                        let relativePoint = result.Tangents.LeftPoint - externalPoint
                        let angle = WoWMathHelper.NormalizeRadian(
                            (float)Math.Atan2(relativePoint.Y, relativePoint.X) - rootAngle)
                        where angle < Math.PI
                        orderby angle descending
                        select result).FirstOrDefault();

            var right = (from result in results
                         let relativePoint = result.Tangents.RightPoint - externalPoint
                         let angle = WoWMathHelper.NormalizeRadian(
                             rootAngle - (float)Math.Atan2(relativePoint.Y, relativePoint.X))
                         where angle < Math.PI
                         orderby angle descending
                         select result).FirstOrDefault();

            return right != null && left != null
                ? new LineClusterTangentPoints(right.Tangents.RightPoint, left.Tangents.LeftPoint,
                    right.Avoid, left.Avoid)
                : null;
        }

        public Vector3 Center
        {
            get
            {
                float centerX = (this.Max(a => a.Location.X + a.Radius) +
                                 this.Min(a => a.Location.X - a.Radius)) / 2f;
                float centerY = (this.Max(a => a.Location.Y + a.Radius) +
                                 this.Min(a => a.Location.Y - a.Radius)) / 2f;
                float centerZ = this.Average(a => a.Location.Z);
                return new Vector3(centerX, centerY, centerZ);
            }
        }
    }
}
