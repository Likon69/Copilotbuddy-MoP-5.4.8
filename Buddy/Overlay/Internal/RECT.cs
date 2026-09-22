using System;

namespace Buddy.Overlay.Internal
{
    internal struct RECT : IEquatable<RECT>
    {
        public bool Equals(RECT other)
        {
            return this.Bottom == other.Bottom && this.Right == other.Right && this.Top == other.Top && this.Left == other.Left;
        }

        public override bool Equals(object obj)
        {
            return obj != null && obj is RECT && this.Equals((RECT)obj);
        }

        public override int GetHashCode()
        {
            return (((((this.Bottom * 397) ^ this.Right) * 397) ^ this.Top) * 397) ^ this.Left;
        }

        public static bool operator ==(RECT left, RECT right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RECT left, RECT right)
        {
            return !left.Equals(right);
        }

        public int Left;

        public int Top;

        public int Right;

        public int Bottom;
    }
}
