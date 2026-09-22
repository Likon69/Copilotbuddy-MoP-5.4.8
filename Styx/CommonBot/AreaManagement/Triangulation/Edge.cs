using System;

namespace Styx.CommonBot.AreaManagement.Triangulation
{
    public struct Edge : IEquatable<Edge>
    {
        public readonly int P1;
        public readonly int P2;

        public Edge(int p1, int p2)
        {
            P1 = p1;
            P2 = p2;
        }

        public bool Equals(Edge other)
        {
            return (P1 == other.P1 && P2 == other.P2) || (P1 == other.P2 && P2 == other.P1);
        }

        public override bool Equals(object obj)
        {
            return !ReferenceEquals(null, obj) && obj.GetType() == typeof(Edge) && Equals((Edge)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (P1 * 397) ^ P2;
            }
        }

        public static bool operator ==(Edge left, Edge right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Edge left, Edge right)
        {
            return !left.Equals(right);
        }
    }
}
