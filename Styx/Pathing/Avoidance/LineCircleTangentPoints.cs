using Tripper.Tools.Math;

namespace Styx.Pathing.Avoidance
{
    public class LineCircleTangentPoints
    {
        public LineCircleTangentPoints(Vector3 rightPoint, Vector3 leftPoint)
        {
            RightPoint = rightPoint;
            LeftPoint = leftPoint;
        }

        public readonly Vector3 RightPoint;
        public readonly Vector3 LeftPoint;
    }
}
