using System;
using System.Collections.Generic;

namespace Styx.WoWInternals.World
{
    public struct Triangle : IEquatable<Triangle>
    {
        public readonly WoWPoint Normal;

        private readonly float _planeDistance;

        public readonly WoWPoint Vertex1;

        public readonly WoWPoint Vertex2;

        public readonly WoWPoint Vertex3;

        public IEnumerable<WoWPoint> Vertices
        {
            get
            {
                yield return Vertex1;
                yield return Vertex2;
                yield return Vertex3;
            }
        }

        public bool Equals(Triangle other)
        {
            return Vertex3.Equals(other.Vertex3) && Vertex2.Equals(other.Vertex2) && Normal.Equals(other.Normal) && _planeDistance.Equals(other._planeDistance) && Vertex1.Equals(other.Vertex1);
        }

        public override bool Equals(object obj)
        {
            return !ReferenceEquals(null, obj) && obj is Triangle && Equals((Triangle)obj);
        }

        public override int GetHashCode()
        {
            int num = Vertex3.GetHashCode();
            num = (num * 397) ^ Vertex2.GetHashCode();
            num = (num * 397) ^ Normal.GetHashCode();
            num = (num * 397) ^ _planeDistance.GetHashCode();
            return (num * 397) ^ Vertex1.GetHashCode();
        }

        public static bool operator ==(Triangle left, Triangle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Triangle left, Triangle right)
        {
            return !left.Equals(right);
        }
    }
}
