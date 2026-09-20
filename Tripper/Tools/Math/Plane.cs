using System;

namespace Tripper.Tools.Math
{
    public struct Plane
    {
        public Plane(Vector3 normal, float d)
        {
            Normal = normal;
            D = d;
        }

        public static Plane Transform(Plane plane, Matrix transformation)
        {
            float x = plane.Normal.X;
            float y = plane.Normal.Y;
            float z = plane.Normal.Z;
            float d = plane.D;
            Matrix.Invert(ref transformation, out transformation);
            Plane result;
            result.Normal.X = (float)((double)x * (double)transformation.M11 + (double)y * (double)transformation.M12 + (double)z * (double)transformation.M13 + (double)d * (double)transformation.M14);
            result.Normal.Y = (float)((double)x * (double)transformation.M21 + (double)y * (double)transformation.M22 + (double)z * (double)transformation.M23 + (double)d * (double)transformation.M24);
            result.Normal.Z = (float)((double)x * (double)transformation.M31 + (double)y * (double)transformation.M32 + (double)z * (double)transformation.M33 + (double)d * (double)transformation.M34);
            result.D = (float)((double)x * (double)transformation.M41 + (double)y * (double)transformation.M42 + (double)z * (double)transformation.M43 + (double)d * (double)transformation.M44);
            return result;
        }

        public override string ToString()
        {
            return string.Format("Normal = {0}, D = {1}", Normal, D);
        }

        public Vector3 Normal;

        public float D;
    }
}
