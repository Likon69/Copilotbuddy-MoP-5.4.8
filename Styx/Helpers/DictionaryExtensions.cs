using System;
using System.Collections.Generic;

namespace Styx.Helpers
{
    public static class DictionaryExtensions
    {
        public static void RemoveAll<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, Func<TValue, bool> predicate)
        {
            List<TKey> keys = null;
            foreach (KeyValuePair<TKey, TValue> pair in dictionary)
            {
                if (!predicate(pair.Value))
                    continue;
                if (keys == null)
                    keys = new List<TKey>();
                keys.Add(pair.Key);
            }

            if (keys != null)
                keys.ForEach(key => dictionary.Remove(key));
        }
    }
}
