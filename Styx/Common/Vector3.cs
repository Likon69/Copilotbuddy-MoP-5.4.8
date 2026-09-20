using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Styx.Common
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Vector3
    {
        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "<{0}, {1}, {2}>", X, Y, Z);
        }

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
                return (float)Math.Sqrt((double)MagnitudeSqr);
            }
        }

        public void Normalize()
        {
            float magnitude = Magnitude;
            X /= magnitude;
            Y /= magnitude;
            Z /= magnitude;
        }

        public Vector2 ToVector2()
        {
            return new Vector2(X, Y);
        }

        public static float Distance(ref Vector3 v1, ref Vector3 v2)
        {
            float num = v1.X - v2.X;
            float num2 = v1.Y - v2.Y;
            float num3 = v1.Z - v2.Z;
            return (float)Math.Sqrt((double)(num * num + num2 * num2 + num3 * num3));
        }

        public float Distance(Vector3 to)
        {
            return Distance(ref this, ref to);
        }

        public float DistanceSqr(Vector3 to)
        {
            return DistanceSqr(ref this, ref to);
        }

        public static float Distance(Vector3 v1, Vector3 v2)
        {
            return Distance(ref v1, ref v2);
        }

        public static float DistanceSqr(ref Vector3 v1, ref Vector3 v2)
        {
            float num = v1.X - v2.X;
            float num2 = v1.Y - v2.Y;
            float num3 = v1.Z - v2.Z;
            return num * num + num2 * num2 + num3 * num3;
        }

        public static float DistanceSqr(Vector3 v1, Vector3 v2)
        {
            return DistanceSqr(ref v1, ref v2);
        }

        public static float Distance2D(ref Vector3 v1, ref Vector3 v2)
        {
            float num = v1.X - v2.X;
            float num2 = v1.Y - v2.Y;
            return (float)Math.Sqrt((double)(num * num + num2 * num2));
        }

        public static float Distance2DSqr(ref Vector3 v1, ref Vector3 v2)
        {
            float num = v1.X - v2.X;
            float num2 = v1.Y - v2.Y;
            return num * num + num2 * num2;
        }

        public float Distance2D(Vector3 to)
        {
            return Distance2D(ref this, ref to);
        }

        public float Distance2DSqr(Vector3 to)
        {
            return Distance2DSqr(ref this, ref to);
        }

        public static float Dot(ref Vector3 v1, ref Vector3 v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
        }

        public static float Dot(Vector3 v1, Vector3 v2)
        {
            return Dot(ref v1, ref v2);
        }

        public static void Cross(ref Vector3 v1, ref Vector3 v2, out Vector3 result)
        {
            result.X = v1.Y * v2.Z - v1.Z * v2.Y;
            result.Y = v1.Z * v2.X - v1.X * v2.Z;
            result.Z = v1.X * v2.Y - v1.Y * v2.X;
        }

        public static Vector3 Cross(Vector3 v1, Vector3 v2)
        {
            Vector3 vector;
            Cross(ref v1, ref v2, out vector);
            return vector;
        }

        public static Vector3 NormalizedDirection(Vector3 start, Vector3 end)
        {
            Vector3 vector = end - start;
            vector.Normalize();
            return vector;
        }

        public static void CatmullRom(ref Vector3 beforeStart, ref Vector3 start, ref Vector3 end, ref Vector3 afterEnd, float amount, out Vector3 result)
        {
            Cardinal(ref beforeStart, ref start, ref end, ref afterEnd, 0.5f, 0.5f, amount, out result);
        }

        public static Vector3 CatmullRom(Vector3 beforeStart, Vector3 start, Vector3 end, Vector3 afterEnd, float amount)
        {
            Vector3 vector;
            Cardinal(ref beforeStart, ref start, ref end, ref afterEnd, 0.5f, 0.5f, amount, out vector);
            return vector;
        }

        public static void Cardinal(ref Vector3 beforeStart, ref Vector3 start, ref Vector3 end, ref Vector3 afterEnd, float aStart, float aEnd, float amount, out Vector3 result)
        {
            Vector3 vector = aStart * (end - beforeStart);
            Vector3 vector2 = aEnd * (afterEnd - start);
            Hermite(ref start, ref vector, ref end, ref vector2, amount, out result);
        }

        public static Vector3 Cardinal(Vector3 beforeStart, Vector3 start, Vector3 end, Vector3 afterEnd, float aStart, float aEnd, float amount)
        {
            Vector3 vector;
            Cardinal(ref beforeStart, ref start, ref end, ref afterEnd, aStart, aEnd, amount, out vector);
            return vector;
        }

        public static void Hermite(ref Vector3 value1, ref Vector3 tangent1, ref Vector3 value2, ref Vector3 tangent2, float amount, out Vector3 result)
        {
            float num = amount * amount;
            float num2 = num * amount;
            float num3 = 2f * num2 - 3f * num + 1f;
            float num4 = -2f * num2 + 3f * num;
            float num5 = num2 - 2f * num + amount;
            float num6 = num2 - num;
            result = value1 * num3 + value2 * num4 + tangent1 * num5 + tangent2 * num6;
        }

        public static Vector3 Hermite(Vector3 value1, Vector3 tangent1, Vector3 value2, Vector3 tangent2, float amount)
        {
            Vector3 vector;
            Hermite(ref value1, ref tangent1, ref value2, ref tangent2, amount, out vector);
            return vector;
        }

        public static Vector3 operator -(Vector3 v)
        {
            return new Vector3(-v.X, -v.Y, -v.Z);
        }

        public static Vector3 operator +(Vector3 left, Vector3 right)
        {
            return new Vector3(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
        }

        public static Vector3 operator -(Vector3 left, Vector3 right)
        {
            return new Vector3(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
        }

        public static Vector3 operator *(Vector3 left, float right)
        {
            left.X *= right;
            left.Y *= right;
            left.Z *= right;
            return left;
        }

        public static Vector3 operator *(float left, Vector3 right)
        {
            right.X *= left;
            right.Y *= left;
            right.Z *= left;
            return right;
        }

        public static Vector3 operator /(Vector3 left, float right)
        {
            left.X /= right;
            left.Y /= right;
            left.Z /= right;
            return left;
        }

        public bool Equals(Vector3 other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        public override bool Equals(object obj)
        {
            return !ReferenceEquals(null, obj) && !(obj.GetType() != typeof(Vector3)) && Equals((Vector3)obj);
        }

        public override int GetHashCode()
        {
            int num = X.GetHashCode();
            num = (num * 397) ^ Y.GetHashCode();
            return (num * 397) ^ Z.GetHashCode();
        }

        public static bool operator ==(Vector3 left, Vector3 right)
        {
            return left.X == right.X && left.Y == right.Y && left.Z == right.Z;
        }

        public static bool operator !=(Vector3 left, Vector3 right)
        {
            return left.X != right.X || left.Y != right.Y || left.Z != right.Z;
        }

        public float X;

        public float Y;

        public float Z;

        public static readonly Vector3 Zero = new Vector3(0f, 0f, 0f);
    }
}
