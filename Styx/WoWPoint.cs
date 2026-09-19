using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Tripper.Tools.Math;

namespace Styx
{
    [StructLayout(LayoutKind.Explicit, Size = 12)]
    public struct WoWPoint
    {
        public WoWPoint(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static implicit operator WoWPoint(Vector3 pnt)
        {
            return new WoWPoint(pnt.X, pnt.Y, pnt.Z);
        }

        public override string ToString()
        {
            return ToString("L");
        }

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

        [FieldOffset(0)]
        public float X;

        [FieldOffset(4)]
        public float Y;

        [FieldOffset(8)]
        public float Z;

        public static readonly WoWPoint Empty;
    }
}
