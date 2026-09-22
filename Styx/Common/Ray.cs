using System;
using System.Runtime.InteropServices;

namespace Styx.Common
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Ray
    {
        public Vector3 Position;
        public Vector3 Direction;

        public Ray(Vector3 position, Vector3 direction)
        {
            Position = position;
            Direction = direction;
        }

        public float? Intersects(Sphere sphere)
        {
            float x = sphere.Center.X - Position.X;
            float y = sphere.Center.Y - Position.Y;
            float z = sphere.Center.Z - Position.Z;
            float distanceSquared = x * x + y * y + z * z;
            float radiusSquared = sphere.Radius * sphere.Radius;
            if (distanceSquared <= radiusSquared)
                return 0f;

            float projection = x * Direction.X + y * Direction.Y + z * Direction.Z;
            if (projection < 0f)
                return null;

            float perpendicularSquared = distanceSquared - projection * projection;
            if (perpendicularSquared > radiusSquared)
                return null;

            float offset = (float)Math.Sqrt(radiusSquared - perpendicularSquared);
            return projection - offset;
        }
    }
}
