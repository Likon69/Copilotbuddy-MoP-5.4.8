using System;
using System.Collections.Generic;
using System.Linq;

namespace Styx.Common
{
    public static class LinqExtensions
    {
        public static bool None<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            return !source.Any(predicate);
        }
    }
}
