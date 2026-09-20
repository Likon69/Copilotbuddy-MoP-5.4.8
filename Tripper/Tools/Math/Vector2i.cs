using System;
using System.Runtime.InteropServices;

namespace Tripper.Tools.Math
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Vector2i : IEquatable<Vector2i>
    {
        public Vector2i(int x, int y)
        {
            X = x;
            Y = y;
        }

        public override string ToString()
        {
            return string.Format("{{{0}, {1}}}", X, Y);
        }

        public override int GetHashCode()
        {
            return X * 397 ^ Y;
        }

        public bool Equals(Vector2i other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj != null && obj is Vector2i && Equals((Vector2i)obj);
        }

        public static bool operator ==(Vector2i left, Vector2i right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Vector2i left, Vector2i right)
        {
            return !left.Equals(right);
        }

        public int X;

        public int Y;
    }
}
