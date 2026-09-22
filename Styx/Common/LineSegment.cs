using System.Runtime.InteropServices;

namespace Styx.Common
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LineSegment
    {
        public Vector3 Start;
        public Vector3 End;

        public LineSegment(Vector3 start, Vector3 end)
        {
            Start = start;
            End = end;
        }

        public float DistanceSqr
        {
            get { return Vector3.DistanceSqr(ref Start, ref End); }
        }

        public float Distance
        {
            get { return Vector3.Distance(ref Start, ref End); }
        }

        public Vector3 Direction
        {
            get { return End - Start; }
        }

        public Vector3 UnitDirection
        {
            get
            {
                Vector3 direction = End - Start;
                direction.Normalize();
                return direction;
            }
        }
    }
}
