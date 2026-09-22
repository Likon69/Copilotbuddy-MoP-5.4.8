using Tripper.Tools.Math;

namespace Styx.Pathing.Avoidance
{
    public class LineClusterTangentPoints : LineCircleTangentPoints
    {
        public LineClusterTangentPoints(Vector3 rightPoint, Vector3 leftPoint, Avoid rightAvoid, Avoid leftAvoid)
            : base(rightPoint, leftPoint)
        {
            RightAvoid = rightAvoid;
            LeftAvoid = leftAvoid;
        }

        public readonly Avoid RightAvoid;
        public readonly Avoid LeftAvoid;
    }
}
