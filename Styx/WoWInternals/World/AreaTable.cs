using System;
using GreenMagic;

namespace Styx.WoWInternals.World
{
    public class AreaTable<T> where T : struct
    {
        private readonly NativeAreaTable _table;

        internal AreaTable(NativeAreaTable table) { _table = table; }

        public int MinX => _table.Bounds.MinX;
        public int MinY => _table.Bounds.MinY;
        public int MaxX => _table.Bounds.MaxX;
        public int MaxY => _table.Bounds.MaxY;
        public int Width => _table.Width;
        public int Height => _table.Height;

        public bool HasEntry(int x, int y) => x >= MinX && x <= MaxX && y >= MinY && y <= MaxY;

        public bool TryGetEntry(int x, int y, out T value)
        {
            if (!HasEntry(x, y))
            {
                value = default(T);
                return false;
            }
            var index = (y - MinY) * Width + x - MinX;
            value = StyxWoW.Memory.Read<T>(_table.Entries + index * MarshalCache<T>.Size);
            return true;
        }

        public T GetEntry(int x, int y)
        {
            T value;
            if (!TryGetEntry(x, y, out value))
                throw new ArgumentOutOfRangeException("x", "X and Y do not represent a valid index in this area table");
            return value;
        }

        public T[] GetEntries() => StyxWoW.Memory.ReadArray<T>(_table.Entries, Width * Height);
    }
}
