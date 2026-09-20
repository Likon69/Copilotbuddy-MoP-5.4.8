using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Tripper.Tools.Math
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Vector3 : IEquatable<Vector3>
    {
        public Vector3(Vector2 v, float z)
        {
            this = new Vector3(v.X, v.Y, z);
        }

        public Vector3(Vector2i v, float z)
        {
            this = new Vector3((float)v.X, (float)v.Y, z);
        }

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float Length()
        {
            return (float)System.Math.Sqrt((double)LengthSqr());
        }

        public float LengthSqr()
        {
            return X * X + Y * Y + Z * Z;
        }

        public void Normalize()
        {
            float divisor = Length();
            Divide(ref this, divisor, out this);
        }

        public float Distance(Vector3 v)
        {
            return Distance(ref this, ref v);
        }

        public float Distance(ref Vector3 v)
        {
            return Distance(ref this, ref v);
        }

        public float DistanceSqr(Vector3 v)
        {
            return DistanceSqr(ref this, ref v);
        }

        public float DistanceSqr(ref Vector3 v)
        {
            return DistanceSqr(ref this, ref v);
        }

        public unsafe void CopyTo(float* arr)
        {
            *arr = X;
            arr[1] = Y;
            arr[2] = Z;
        }

        public Vector3 Offset(float x, float y, float z)
        {
            return new Vector3(X + x, Y + y, Z + z);
        }

        public bool Equals(Vector3 other)
        {
            return Equals(ref this, ref other);
        }

        public bool Equals(ref Vector3 other)
        {
            return Equals(ref this, ref other);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            bool result;
            try
            {
                result = Equals((Vector3)obj);
            }
            catch (InvalidCastException)
            {
                result = false;
            }

            return result;
        }

        public static bool operator ==(Vector3 ls, Vector3 rs)
        {
            return Equals(ref ls, ref rs);
        }

        public static bool operator !=(Vector3 ls, Vector3 rs)
        {
            return !Equals(ref ls, ref rs);
        }

        public static bool Equals(Vector3 v1, Vector3 v2)
        {
            return Equals(ref v1, ref v2);
        }

        public static bool Equals(ref Vector3 v1, ref Vector3 v2)
        {
            return v1.X == v2.X && v1.Y == v2.Y && v1.Z == v2.Z;
        }

        public override int GetHashCode()
        {
            return (X.GetHashCode() * 397 ^ Y.GetHashCode()) * 397 ^ Z.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{{{0}, {1}, {2}}}", X, Y, Z);
        }

        public static Vector3 operator -(Vector3 v)
        {
            v.X = -v.X;
            v.Y = -v.Y;
            v.Z = -v.Z;
            return v;
        }

        public static Vector3 operator +(Vector3 ls, Vector3 rs)
        {
            Vector3 result;
            Add(ref ls, ref rs, out result);
            return result;
        }

        public static Vector3 operator -(Vector3 ls, Vector3 rs)
        {
            Vector3 result;
            Subtract(ref ls, ref rs, out result);
            return result;
        }

        public static Vector3 operator *(Vector3 ls, Vector3 rs)
        {
            Vector3 result;
            Multiply(ref ls, ref rs, out result);
            return result;
        }

        public static Vector3 operator *(Vector3 ls, float rs)
        {
            Vector3 result;
            Multiply(ref ls, rs, out result);
            return result;
        }

        public static Vector3 operator /(Vector3 ls, Vector3 rs)
        {
            Vector3 result;
            Multiply(ref ls, ref rs, out result);
            return result;
        }

        public static Vector3 operator /(Vector3 ls, float rs)
        {
            Vector3 result;
            Divide(ref ls, rs, out result);
            return result;
        }

        public static void Add(ref Vector3 v1, ref Vector3 v2, out Vector3 result)
        {
            result.X = v1.X + v2.X;
            result.Y = v1.Y + v2.Y;
            result.Z = v1.Z + v2.Z;
        }

        public static void Subtract(ref Vector3 v1, ref Vector3 v2, out Vector3 result)
        {
            result.X = v1.X - v2.X;
            result.Y = v1.Y - v2.Y;
            result.Z = v1.Z - v2.Z;
        }

        public static void Multiply(ref Vector3 v1, ref Vector3 v2, out Vector3 result)
        {
            result.X = v1.X * v2.X;
            result.Y = v1.Y * v2.Y;
            result.Z = v1.Z * v2.Z;
        }

        public static void Multiply(ref Vector3 v1, float scalar, out Vector3 result)
        {
            result.X = v1.X * scalar;
            result.Y = v1.Y * scalar;
            result.Z = v1.Z * scalar;
        }

        public static void Divide(ref Vector3 v1, ref Vector3 v2, out Vector3 result)
        {
            result.X = v1.X / v2.X;
            result.Y = v1.Y / v2.Y;
            result.Z = v1.Z / v2.Z;
        }

        public static void Divide(ref Vector3 v1, float divisor, out Vector3 result)
        {
            result.X = v1.X / divisor;
            result.Y = v1.Y / divisor;
            result.Z = v1.Z / divisor;
        }

        public static void Negate(ref Vector3 v, out Vector3 result)
        {
            result.X = -v.X;
            result.Y = -v.Y;
            result.Z = -v.Z;
        }

        public static float Distance(Vector3 v1, Vector3 v2)
        {
            return (float)System.Math.Sqrt((double)DistanceSqr(ref v1, ref v2));
        }

        public static float Distance(ref Vector3 v1, ref Vector3 v2)
        {
            return (float)System.Math.Sqrt((double)DistanceSqr(ref v1, ref v2));
        }

        public static float DistanceSqr(Vector3 v1, Vector3 v2)
        {
            return DistanceSqr(ref v1, ref v2);
        }

        public static float DistanceSqr(ref Vector3 v1, ref Vector3 v2)
        {
            float num = v1.X - v2.X;
            float num2 = v1.Y - v2.Y;
            float num3 = v1.Z - v2.Z;
            return num * num + num2 * num2 + num3 * num3;
        }

        public static Vector3 GetDirection(Vector3 from, Vector3 to)
        {
            Vector3 result;
            GetDirection(ref from, ref to, out result);
            return result;
        }

        public static void GetDirection(ref Vector3 from, ref Vector3 to, out Vector3 dir)
        {
            Subtract(ref to, ref from, out dir);
            dir.Normalize();
        }

        public static float Dot(ref Vector3 v1, ref Vector3 v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
        }

        public static float Dot(Vector3 v1, Vector3 v2)
        {
            return Dot(ref v1, ref v2);
        }

        public static Vector3 Cross(Vector3 v1, Vector3 v2)
        {
            Vector3 result;
            Cross(ref v1, ref v2, out result);
            return result;
        }

        public static void Cross(ref Vector3 v1, ref Vector3 v2, out Vector3 crossProduct)
        {
            crossProduct.X = v1.Y * v2.Z - v1.Z * v2.Y;
            crossProduct.Y = v1.Z * v2.X - v1.X * v2.Z;
            crossProduct.Z = v1.X * v2.Y - v1.Y * v2.X;
        }

        public static void Transform(ref Vector3 v, ref Matrix m, out Vector3 result)
        {
            float x = v.X * m.M11 + v.Y * m.M21 + v.Z * m.M31 + m.M41;
            float y = v.X * m.M12 + v.Y * m.M22 + v.Z * m.M32 + m.M42;
            float z = v.X * m.M13 + v.Y * m.M23 + v.Z * m.M33 + m.M43;
            result.X = x;
            result.Y = y;
            result.Z = z;
        }

        public static Vector3 Transform(Vector3 position, Matrix matrix)
        {
            float x = position.X * matrix.M11 + position.Y * matrix.M21 + position.Z * matrix.M31 + matrix.M41;
            float y = position.X * matrix.M12 + position.Y * matrix.M22 + position.Z * matrix.M32 + matrix.M42;
            float z = position.X * matrix.M13 + position.Y * matrix.M23 + position.Z * matrix.M33 + matrix.M43;
            Vector3 result;
            result.X = x;
            result.Y = y;
            result.Z = z;
            return result;
        }

        public static void TransformNormal(ref Vector3 normal, ref Matrix transform, out Vector3 result)
        {
            result = new Vector3(normal.X * transform.M11 + normal.Y * transform.M21 + normal.Z * transform.M31, normal.X * transform.M12 + normal.Y * transform.M22 + normal.Z * transform.M32, normal.X * transform.M13 + normal.Y * transform.M23 + normal.Z * transform.M33);
        }

        public static Vector3 TransformNormal(Vector3 normal, Matrix transform)
        {
            Vector3 result;
            TransformNormal(ref normal, ref transform, out result);
            return result;
        }

        public static void Min(ref Vector3 v1, ref Vector3 v2, out Vector3 result)
        {
            result.X = System.Math.Min(v1.X, v2.X);
            result.Y = System.Math.Min(v1.Y, v2.Y);
            result.Z = System.Math.Min(v1.Z, v2.Z);
        }

        public static Vector3 Min(Vector3 v1, Vector3 v2)
        {
            Vector3 result;
            Min(ref v1, ref v2, out result);
            return result;
        }

        public static void Max(ref Vector3 v1, ref Vector3 v2, out Vector3 result)
        {
            result.X = System.Math.Max(v1.X, v2.X);
            result.Y = System.Math.Max(v1.Y, v2.Y);
            result.Z = System.Math.Max(v1.Z, v2.Z);
        }

        public static Vector3 Max(Vector3 v1, Vector3 v2)
        {
            Vector3 result;
            Max(ref v1, ref v2, out result);
            return result;
        }

        public static bool CalculateBarycentric(ref Vector3 p1, ref Vector3 p2, ref Vector3 p3, ref Vector3 point, out Vector3 barycentric)
        {
            float det = p1.X * p2.Y - p1.X * p3.Y - p2.X * p1.Y + p2.X * p3.Y + p3.X * p1.Y - p3.X * p2.Y;
            if (det == 0f)
            {
                barycentric = Zero;
                return false;
            }

            float detU = point.X * p2.Y - point.X * p3.Y - p2.X * point.Y + p2.X * p3.Y + p3.X * point.Y - p3.X * p2.Y;
            barycentric.X = detU / det;
            float detV = p1.X * point.Y - p1.X * p3.Y - point.X * p1.Y + point.X * p3.Y + p3.X * p1.Y - p3.X * point.Y;
            barycentric.Y = detV / det;
            float detW = p1.X * p2.Y - p1.X * point.Y - p2.X * p1.Y + p2.X * point.Y + point.X * p1.Y - point.X * p2.Y;
            barycentric.Z = detW / det;

            return true;
        }

        public static Vector3 Zero { get; private set; } = new Vector3(0f, 0f, 0f);

        public static Vector3 One { get; private set; } = new Vector3(1f, 1f, 1f);

        public float MagnitudeSqr
        {
            get
            {
                return X * X + Y * Y + Z * Z;
            }
        }

        public float Magnitude
        {
            get
            {
                return (float)System.Math.Sqrt((double)MagnitudeSqr);
            }
        }

        public float X;

        public float Y;

        public float Z;
    }
}
