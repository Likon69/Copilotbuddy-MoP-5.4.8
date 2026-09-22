using System;
using Tripper.Tools.Math;

namespace Tripper.MeshMisc
{
    public struct TileIdentifier : IEquatable<TileIdentifier>
    {
        public int X;
        public int Y;

        public TileIdentifier(int x, int y) { X = x; Y = y; }
        public bool Equals(TileIdentifier other) => other.X == X && other.Y == Y;
        public override int GetHashCode() => Y * 64 + X;
        public override bool Equals(object obj) => obj is TileIdentifier && Equals((TileIdentifier)obj);
        public override string ToString() => string.Format("<{0}, {1}>", X, Y);

        public string ToString(string format)
        {
            var lower = format.ToLower();
            if (!lower.Contains("x") && !lower.Contains("y"))
                throw new FormatException("The format " + format + " does not contain X or Y.");
            return format.Replace("X", X.ToString()).Replace("x", X.ToString())
                .Replace("Y", Y.ToString()).Replace("y", Y.ToString());
        }

        public static bool operator ==(TileIdentifier left, TileIdentifier right) => left.Equals(right);
        public static bool operator !=(TileIdentifier left, TileIdentifier right) => !left.Equals(right);
        public static TileIdentifier GetByPosition(Vector3 position) => GetByPosition(position.X, position.Y);
        public static TileIdentifier GetByPosition(ref Vector3 position) => GetByPosition(position.X, position.Y);
        public static TileIdentifier GetByPosition(Vector2 position) => GetByPosition(position.X, position.Y);

        public static TileIdentifier GetByPosition(float x, float y)
        {
            return new TileIdentifier(32 - (int)Math.Ceiling(y / 533.3333f), 32 - (int)Math.Ceiling(x / 533.3333f));
        }

        public static TileIdentifier GetDetourTile(TileIdentifier wowTile, int subX, int subY)
        {
            return new TileIdentifier((wowTile.X - 32) * 4 + subX, (wowTile.Y - 32) * 4 + subY);
        }

        public static TileIdentifier GetWowTile(TileIdentifier detourTile)
        {
            var subX = detourTile.X % 4;
            var subY = detourTile.Y % 4;
            if (subX < 0) subX += 4;
            if (subY < 0) subY += 4;
            return new TileIdentifier((detourTile.X - subX) / 4 + 32, (detourTile.Y - subY) / 4 + 32);
        }

        public static implicit operator Vector2i(TileIdentifier tile) => new Vector2i(tile.X, tile.Y);
        public static implicit operator TileIdentifier(Vector2i value) => new TileIdentifier(value.X, value.Y);
    }
}
