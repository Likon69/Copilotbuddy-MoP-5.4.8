using System;
using System.Globalization;

namespace Styx.Common
{
    public struct Vector2 : IEquatable<Vector2>
    {
        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float Length()
        {
            return (float)Math.Sqrt((double)LengthSqr());
        }

        public float LengthSqr()
        {
            return X * X + Y * Y;
        }

        public void Normalize()
        {
            float num = Length();
            Divide(ref this, num, out this);
        }

        public float Distance(Vector2 v)
        {
            return Distance(ref this, ref v);
        }

        public float Distance(ref Vector2 v)
        {
            return Distance(ref this, ref v);
        }

        public float DistanceSqr(Vector2 v)
        {
            return DistanceSqr(ref this, ref v);
        }

        public float DistanceSqr(ref Vector2 v)
        {
            return DistanceSqr(ref this, ref v);
        }

        public Vector3 ToVector3()
        {
            return new Vector3(X, Y, 0f);
        }

        public bool Equals(Vector2 other)
        {
            return Equals(ref this, ref other);
        }

        public bool Equals(ref Vector2 other)
        {
            return Equals(ref this, ref other);
        }

        public static bool Equals(ref Vector2 v1, ref Vector2 v2)
        {
            return v1.X == v2.X && v1.Y == v2.Y;
        }

        public static bool operator ==(Vector2 ls, Vector2 rs)
        {
            return Equals(ref ls, ref rs);
        }

        public static bool operator !=(Vector2 ls, Vector2 rs)
        {
            return !Equals(ref ls, ref rs);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            bool flag;
            try
            {
                flag = Equals((Vector2)obj);
            }
            catch (InvalidCastException)
            {
                flag = false;
            }

            return flag;
        }

        public override int GetHashCode()
        {
            return (X.GetHashCode() * 397) ^ Y.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{{{0}, {1}}}", X, Y);
        }

        public static Vector2 operator +(Vector2 ls, Vector2 rs)
        {
            Vector2 vector;
            Add(ref ls, ref rs, out vector);
            return vector;
        }

        public static Vector2 operator -(Vector2 ls, Vector2 rs)
        {
            Vector2 vector;
            Subtract(ref ls, ref rs, out vector);
            return vector;
        }

        public static Vector2 operator -(Vector2 v)
        {
            v.X = -v.X;
            v.Y = -v.Y;
            return v;
        }

        public static Vector2 operator *(Vector2 ls, Vector2 rs)
        {
            Vector2 vector;
            Multiply(ref ls, ref rs, out vector);
            return vector;
        }

        public static Vector2 operator *(Vector2 ls, float rs)
        {
            Vector2 vector;
            Multiply(ref ls, rs, out vector);
            return vector;
        }

        public static Vector2 operator /(Vector2 ls, Vector2 rs)
        {
            Vector2 vector;
            Multiply(ref ls, ref rs, out vector);
            return vector;
        }

        public static Vector2 operator /(Vector2 ls, float rs)
        {
            Vector2 vector;
            Divide(ref ls, rs, out vector);
            return vector;
        }

        public static void Add(ref Vector2 v1, ref Vector2 v2, out Vector2 result)
        {
            result.X = v1.X + v2.X;
            result.Y = v1.Y + v2.Y;
        }

        public static void Subtract(ref Vector2 v1, ref Vector2 v2, out Vector2 result)
        {
            result.X = v1.X - v2.X;
            result.Y = v1.Y - v2.Y;
        }

        public static void Multiply(ref Vector2 v1, ref Vector2 v2, out Vector2 result)
        {
            result.X = v1.X * v2.X;
            result.Y = v1.Y * v2.Y;
        }

        public static void Multiply(ref Vector2 v1, float scalar, out Vector2 result)
        {
            result.X = v1.X * scalar;
            result.Y = v1.Y * scalar;
        }

        public static void Divide(ref Vector2 v1, ref Vector2 v2, out Vector2 result)
        {
            result.X = v1.X / v2.X;
            result.Y = v1.Y / v2.Y;
        }

        public static void Divide(ref Vector2 v1, float divisor, out Vector2 result)
        {
            float num = 1f / divisor;
            Multiply(ref v1, num, out result);
        }

        public static float Distance(ref Vector2 v1, ref Vector2 v2)
        {
            return (float)Math.Sqrt((double)DistanceSqr(ref v1, ref v2));
        }

        public static float DistanceSqr(ref Vector2 v1, ref Vector2 v2)
        {
            float num = v1.X - v2.X;
            float num2 = v1.Y - v2.Y;
            return num * num + num2 * num2;
        }

        public static void GetDirection(ref Vector2 from, ref Vector2 to, out Vector2 dir)
        {
            Subtract(ref to, ref from, out dir);
            dir.Normalize();
        }

        public static Vector2 Min(Vector2 v1, Vector2 v2)
        {
            Vector2 vector;
            Min(ref v1, ref v2, out vector);
            return vector;
        }

        public static void Min(ref Vector2 v1, ref Vector2 v2, out Vector2 result)
        {
            result.X = Math.Min(v1.X, v2.X);
            result.Y = Math.Min(v1.Y, v2.Y);
        }

        public static Vector2 Max(Vector2 v1, Vector2 v2)
        {
            Vector2 vector;
            Max(ref v1, ref v2, out vector);
            return vector;
        }

        public static void Max(ref Vector2 v1, ref Vector2 v2, out Vector2 result)
        {
            result.X = Math.Max(v1.X, v2.X);
            result.Y = Math.Max(v1.Y, v2.Y);
        }

        public static Vector2 Zero { get; private set; } = new Vector2(0f, 0f);

        public static Vector2 One { get; private set; } = new Vector2(1f, 1f);

        public static float Dot(Vector2 v1, Vector2 v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y;
        }

        public float X;

        public float Y;
    }
}
