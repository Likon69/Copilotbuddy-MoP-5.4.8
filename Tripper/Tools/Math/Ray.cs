using System;
using System.Runtime.InteropServices;

namespace Tripper.Tools.Math
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Ray
    {
        public Ray(Vector3 position, Vector3 direction)
        {
            Position = position;
            Direction = direction;
        }

        public float? Intersects(BoundingSphere sphere)
        {
            float x = sphere.Center.X - Position.X;
            float y = sphere.Center.Y - Position.Y;
            float z = sphere.Center.Z - Position.Z;
            float distanceSqr = x * x + y * y + z * z;
            float radiusSqr = sphere.Radius * sphere.Radius;
            if (distanceSqr <= radiusSqr)
            {
                return new float?(0f);
            }

            float projection = x * Direction.X + y * Direction.Y + z * Direction.Z;
            if (projection < 0f)
            {
                return null;
            }

            float offsetSqr = distanceSqr - projection * projection;
            if (offsetSqr > radiusSqr)
            {
                return null;
            }

            float halfChord = (float)System.Math.Sqrt((double)(radiusSqr - offsetSqr));
            return new float?(projection - halfChord);
        }

        public Vector3 Position;

        public Vector3 Direction;
    }
}
