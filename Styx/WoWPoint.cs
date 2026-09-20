using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using Styx.Common;
using Tripper.Tools.Math;

namespace Styx
{
    [StructLayout(LayoutKind.Explicit, Size = 12)]
    public struct WoWPoint : IEquatable<WoWPoint>, IFormattable, IRangeAble
    {
        #region Properties

        public float Length
        {
            get
            {
                return Distance(Center);
            }
        }

        [FieldOffset(0)]
        public float X;

        [FieldOffset(4)]
        public float Y;

        [FieldOffset(8)]
        public float Z;

        public float ComponentSum { get { return X + Y + Z; } }

        #endregion

        #region Constructors

        public WoWPoint(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public WoWPoint(double x, double y, double z)
            : this()
        {
            X = Convert.ToSingle(x, CultureInfo.InvariantCulture);
            Y = Convert.ToSingle(y, CultureInfo.InvariantCulture);
            Z = Convert.ToSingle(z, CultureInfo.InvariantCulture);
        }

        public WoWPoint(float value)
            : this(value, value, value)
        {
        }

        #endregion

        #region Instance methods

        public float Distance(WoWPoint other)
        {
            float xDiff = X - other.X;
            float yDiff = Y - other.Y;
            float zDiff = Z - other.Z;

            return (float)Math.Sqrt(xDiff * xDiff + yDiff * yDiff + zDiff * zDiff);
        }

        public float DistanceSqr(WoWPoint other)
        {
            float dx = X - other.X;
            float dy = Y - other.Y;
            float dz = Z - other.Z;

            return dx * dx + dy * dy + dz * dz;
        }

        public float Distance2D(WoWPoint other)
        {
            float xDiff = X - other.X;
            float yDiff = Y - other.Y;

            return (float)Math.Sqrt(xDiff * xDiff + yDiff * yDiff);
        }

        public float Distance2DSqr(WoWPoint other)
        {
            float dx = X - other.X;
            float dy = Y - other.Y;

            return dx * dx + dy * dy;
        }

        public void Normalize()
        {
            float num = 1f / Length;

            X *= num;
            Y *= num;
            Z *= num;
        }

        public void MakePositive()
        {
            X = X < 0 ? -X : X;
            Y = Y < 0 ? -Y : Y;
            Z = Z < 0 ? -Z : Z;
        }

        public void MakeNegative()
        {
            X = X > 0 ? -X : X;
            Y = Y > 0 ? -Y : Y;
            Z = Z > 0 ? -Z : Z;
        }

        public void Negate()
        {
            X = -X;
            Y = -Y;
            Z = -Z;
        }

        public WoWPoint Add(float xOffset, float yOffset, float zOffset)
        {
            return new WoWPoint(X + xOffset, Y + yOffset, Z + zOffset);
        }

        public WoWPoint RayCast(float headingRadians, float distance)
        {
            return RayCast(this, headingRadians, distance);
        }

        public WoWPoint GetDirectionTo(WoWPoint to)
        {
            return GetDirection(this, to);
        }

        public Styx.Common.Range GetRange()
        {
            float magnitude = Length;
            var min = (int)Math.Floor(magnitude);
            var max = (int)Math.Ceiling(magnitude);
            if (min == max)
                max++;

            return new Styx.Common.Range(min, max);
        }

        internal List<WoWPoint> GetPointsAtDistanceOf(float dist, RangedDictionary<WoWPoint> points)
        {
            float magnitude = Length;
            List<WoWPoint> candidates = points.GetByBigRange(new Styx.Common.Range((int)Math.Floor(magnitude - dist), (int)Math.Ceiling(magnitude + dist)));
            if (candidates.Count == 0)
                return candidates;

            float distSqr = dist * dist;

            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                if (DistanceSqr(candidates[i]) > distSqr)
                    candidates.RemoveAt(i);
            }

            return candidates;
        }

        #endregion

        #region Statics

        public static WoWPoint RayCast(WoWPoint from, float headingRadians, float distance)
        {
            return new WoWPoint(from.X + ((float)Math.Cos(-headingRadians) * distance), from.Y + ((float)Math.Sin(headingRadians) * distance), from.Z);
        }

        public static WoWPoint GetDirection(WoWPoint from, WoWPoint to)
        {
            WoWPoint direction = to - from;
            direction.Normalize();
            return direction;
        }

        public static readonly WoWPoint Center;

        public static readonly WoWPoint Zero;

        public static readonly WoWPoint Empty;

        public static readonly WoWPoint MinValue = new WoWPoint(float.MinValue);

        public static readonly WoWPoint MaxValue = new WoWPoint(float.MaxValue);

        public static readonly WoWPoint XUnit = new WoWPoint(1, 0, 0);

        public static readonly WoWPoint YUnit = new WoWPoint(0, 1, 0);

        public static readonly WoWPoint ZUnit = new WoWPoint(0, 0, 1);

        #endregion

        #region Operators

        public static WoWPoint operator +(WoWPoint pnt1, WoWPoint pnt2)
        {
            return new WoWPoint(pnt1.X + pnt2.X, pnt1.Y + pnt2.Y, pnt1.Z + pnt2.Z);
        }

        public static WoWPoint operator -(WoWPoint pnt1, WoWPoint pnt2)
        {
            return new WoWPoint(pnt1.X - pnt2.X, pnt1.Y - pnt2.Y, pnt1.Z - pnt2.Z);
        }

        public static WoWPoint operator *(WoWPoint pnt1, WoWPoint pnt2)
        {
            return new WoWPoint(pnt1.X * pnt2.X, pnt1.Y * pnt2.Y, pnt1.Z * pnt2.Z);
        }

        public static WoWPoint operator /(WoWPoint pnt1, WoWPoint pnt2)
        {
            return new WoWPoint(pnt1.X / pnt2.X, pnt1.Y / pnt2.Y, pnt1.Z / pnt2.Z);
        }

        public static WoWPoint operator +(WoWPoint pnt, float val)
        {
            return new WoWPoint(pnt.X + val, pnt.Y + val, pnt.Z + val);
        }

        public static WoWPoint operator -(WoWPoint pnt, float val)
        {
            return new WoWPoint(pnt.X - val, pnt.Y - val, pnt.Z - val);
        }

        public static WoWPoint operator *(WoWPoint pnt, float scalar)
        {
            return new WoWPoint(pnt.X * scalar, pnt.Y * scalar, pnt.Z * scalar);
        }

        public static WoWPoint operator /(WoWPoint pnt, float scalar)
        {
            return new WoWPoint(pnt.X / scalar, pnt.Y / scalar, pnt.Z / scalar);
        }

        public static bool operator ==(WoWPoint pnt1, WoWPoint pnt2)
        {
            return pnt1.Equals(pnt2);
        }

        public static bool operator !=(WoWPoint pnt1, WoWPoint pnt2)
        {
            return !(pnt1 == pnt2);
        }

        public static implicit operator Tripper.Tools.Math.Vector3(WoWPoint pnt)
        {
            return new Tripper.Tools.Math.Vector3(pnt.X, pnt.Y, pnt.Z);
        }

        public static implicit operator WoWPoint(Tripper.Tools.Math.Vector3 pnt)
        {
            return new WoWPoint(pnt.X, pnt.Y, pnt.Z);
        }

        public static implicit operator Styx.Common.Vector3(WoWPoint pnt)
        {
            return new Styx.Common.Vector3(pnt.X, pnt.Y, pnt.Z);
        }

        public static implicit operator WoWPoint(Styx.Common.Vector3 pnt)
        {
            return new WoWPoint(pnt.X, pnt.Y, pnt.Z);
        }

        #endregion

        #region Overrides

        public override string ToString()
        {
            return ToString("L");
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            try
            {
                if (((WoWPoint)obj).Equals(this))
                {
                    return true;
                }
            }
            catch (InvalidCastException)
            {
            }

            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = X.GetHashCode();
                result = (result * 397) ^ Y.GetHashCode();
                result = (result * 397) ^ Z.GetHashCode();
                return result;
            }
        }

        #endregion

        #region IEquatable<WoWPoint> Members

        public bool Equals(WoWPoint other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        #endregion

        #region IFormattable Members

        public string ToString(string format)
        {
            return ToString(format, CultureInfo.CurrentCulture);
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(format))
            {
                format = "L";
            }

            if (format.ToUpperInvariant() == "L")
            {
                return string.Format(formatProvider, "<{0}, {1}, {2}>", X, Y, Z);
            }

            return ToString("L");
        }

        #endregion
    }
}
