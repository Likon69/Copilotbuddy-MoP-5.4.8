using System;
using System.Collections.Generic;
using System.Linq;
using Styx.Helpers;
using Tripper.Tools.Math;

namespace Styx.Pathing.Avoidance
{
    /// <summary>Contains a group of overlaping avoids.</summary>
    public class AvoidCluster : List<Avoid>
    {
        public AvoidCluster(params Avoid[] avoids)
        {
            AddRange(avoids);
        }

        public LineClusterTangentPoints GetLineTangentPoints(Vector3 externalPoint)
        {
            var resultList = (from avoid in this
                               let result = Helpers.GetLineCircleTangentPoints(avoid.Location, avoid.Radius + Helpers.SubPadding, externalPoint)
                               where result != null
                               select new { Avoid = avoid, Tangents = result }).ToArray();

            if (resultList.Length > 0)
            {
                Vector3 centerRelative = Center - externalPoint;
                var rootAngle = (float)Math.Atan2(centerRelative.Y, centerRelative.X);

                var left = (from result in resultList
                            let relativePoint = result.Tangents.LeftPoint - externalPoint
                            let angle = WoWMathHelper.NormalizeRadian((float)Math.Atan2(relativePoint.Y, relativePoint.X) - rootAngle)
                            where angle < Math.PI
                            orderby angle descending
                            select result).FirstOrDefault();

                var right = (from result in resultList
                             let relativePoint = result.Tangents.RightPoint - externalPoint
                             let angle = WoWMathHelper.NormalizeRadian(rootAngle - (float)Math.Atan2(relativePoint.Y, relativePoint.X))
                             where angle < Math.PI
                             orderby angle descending
                             select result).FirstOrDefault();

                if (right != null && left != null)
                {
                    return new LineClusterTangentPoints(right.Tangents.RightPoint, left.Tangents.LeftPoint, right.Avoid, left.Avoid);
                }
                return null;
            }
            return null;
        }

        public Vector3 Center
        {
            get
            {
                float centerX = (this.Max(a => a.Location.X + a.Radius) + this.Min(a => a.Location.X - a.Radius)) / 2;
                float centerY = (this.Max(a => a.Location.Y + a.Radius) + this.Min(a => a.Location.Y - a.Radius)) / 2;
                float centerZ = this.Average(a => a.Location.Z);
                return new Vector3(centerX, centerY, centerZ);
            }
        }
    }
}
