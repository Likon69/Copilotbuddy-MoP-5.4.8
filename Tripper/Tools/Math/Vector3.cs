using System.Globalization;
using System.Runtime.InteropServices;

namespace Tripper.Tools.Math
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
            return string.Format(CultureInfo.InvariantCulture, "{{{0}, {1}, {2}}}", X, Y, Z);
        }

        public float X;

        public float Y;

        public float Z;
    }
}
