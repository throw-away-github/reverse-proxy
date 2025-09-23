using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.OutputCaching;

namespace CF.AccessProxy.Extensions;

public static class OutputCacheExtensions
{
    private const string GET_CACHE_KEY = "get_" + nameof(CacheKey);

    public static string? CacheKey(this OutputCacheContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return GetPrivateCacheKeyProperty(context);
    }

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = GET_CACHE_KEY)]
    private static extern string? GetPrivateCacheKeyProperty(OutputCacheContext c);

    public static TimeSpan CachedEntryAge(this OutputCacheContext context)
    {
        return GetCachedEntryAge(context);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = $"get_{nameof(CachedEntryAge)}")]
        static extern TimeSpan GetCachedEntryAge(OutputCacheContext c);
    }
}