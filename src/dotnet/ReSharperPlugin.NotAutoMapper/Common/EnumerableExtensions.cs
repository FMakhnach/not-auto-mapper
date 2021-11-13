using System;
using System.Collections.Generic;
using System.Linq;

namespace ReSharperPlugin.NotAutoMapper.Common
{
    public static class EnumerableExtensions
    {
        public static (IReadOnlyCollection<T>, IReadOnlyCollection<T>) SplitBy<T>(this IEnumerable<T> collection, Predicate<T> predicate)
        {
            var split = collection.ToLookup(x => predicate(x));

            var succeeded = split.Contains(true) ? split[true].ToArray() : Array.Empty<T>();
            var failed = split.Contains(false) ? split[false].ToArray() : Array.Empty<T>();

            return (succeeded, failed);
        }
        
        public static IDictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> collection)
        {
            return collection.ToDictionary(x => x.Key, x => x.Value);
        }
    }
}