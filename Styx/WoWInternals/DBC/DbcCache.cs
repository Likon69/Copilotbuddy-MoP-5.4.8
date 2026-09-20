using System;
using System.Collections.Generic;
using Styx.Patchables;

namespace Styx.WoWInternals.DBC
{
    internal static class DbcCache<T>
    {
        private static readonly Dictionary<uint, T> _cache = new Dictionary<uint, T>();

        public static T GetOrAdd(ClientDb clientDb, uint id, Func<WoWDb.Row, T> factory)
        {
            T value;
            if (!_cache.TryGetValue(id, out value))
            {
                WoWDb.Row row = StyxWoW.Db[clientDb].GetRow(id);
                if (row != null && row.IsValid)
                {
                    value = factory(row);
                }
                _cache[id] = value;
            }
            return value;
        }
    }
}
