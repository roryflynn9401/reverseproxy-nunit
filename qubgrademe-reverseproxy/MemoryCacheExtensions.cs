using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace qubgrademe_reverseproxy
{
    public static class MemoryCacheExtensions
    {
        private static readonly Lazy<Func<MemoryCache, object>> GetEntries6 =
            new Lazy<Func<MemoryCache, object>>(() => (Func<MemoryCache, object>)Delegate.CreateDelegate(
                typeof(Func<MemoryCache, object>),
                typeof(MemoryCache).GetProperty("EntriesCollection", BindingFlags.NonPublic | BindingFlags.Instance).GetGetMethod(true),
                throwOnBindFailure: true));

        private static readonly Func<MemoryCache, IDictionary> GetEntries;

        static MemoryCacheExtensions() => GetEntries = cache => (IDictionary)GetEntries6.Value(cache);               

        public static ICollection GetKeys(this IMemoryCache memoryCache) =>
            GetEntries((MemoryCache)memoryCache).Keys;

        public static IEnumerable<T> GetKeys<T>(this IMemoryCache memoryCache) =>
            memoryCache.GetKeys().OfType<T>();
    }
}