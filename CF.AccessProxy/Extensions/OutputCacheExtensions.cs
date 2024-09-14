using System.Buffers;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.OutputCaching;

namespace CF.AccessProxy.Extensions;

public static class OutputCacheExtensions
{
    private const string GET = "get_";
    private const string CACHE_KEY = GET + nameof(CacheKey);

    private static readonly Lazy<Func<OutputCacheContext, ReadOnlySequence<byte>>> _getPrivateCachedResponseBody;

    private static readonly Lazy<Func<string, IOutputCacheStore, CancellationToken, ReadOnlySequence<byte>>> _getCacheEntry;
    private static readonly Lazy<Func<object, ReadOnlySequence<byte>>> _getResponseBodyFromCacheEntry;

    static OutputCacheExtensions()
    {
        var outputCacheEntryProperty = CreateLazy(() =>
        {
            var property = typeof(OutputCacheContext).GetProperty("CachedResponse", BindingFlags.Instance | BindingFlags.NonPublic);
            if (property == null)
            {
                throw new TypeAccessException("Could not find the private CachedResponse property.");
            }
            return property;
        });

        var cachedResponseBodyProperty = CreateLazy(() =>
        {
            var outputCacheEntryType = outputCacheEntryProperty.Value.PropertyType;
            var property = outputCacheEntryType.GetProperty("Body", BindingFlags.Instance | BindingFlags.Public);
            if (property == null)
            {
                throw new InvalidOperationException("Could not find the private CachedResponse.Body property.");
            }
            return property;
        });

        _getPrivateCachedResponseBody = CreateLazy(() =>
        {
            var property = outputCacheEntryProperty.Value;
            var bodyProperty = cachedResponseBodyProperty.Value;
            var parameter = Expression.Parameter(typeof(OutputCacheContext));
            var bodyExpression = Expression.MakeMemberAccess(Expression.MakeMemberAccess(parameter, property), bodyProperty);
            var lambda = Expression.Lambda<Func<OutputCacheContext, ReadOnlySequence<byte>>>(bodyExpression, parameter);
            return lambda.Compile();
        });

        _getCacheEntry = CreateLazy(() =>
        {
            // internal static class OutputCacheEntryFormatter
            // public static async ValueTask<OutputCacheEntry?> GetAsync(string key, IOutputCacheStore store, CancellationToken cancellationToken)
            var type = typeof(OutputCacheContext).Assembly.GetType("Microsoft.AspNetCore.OutputCaching.OutputCacheEntryFormatter");
            if (type == null)
            {
                throw new TypeAccessException("Could not find the OutputCacheEntryFormatter type.");
            }
            
            var method = type.GetMethod("GetAsync", BindingFlags.Public | BindingFlags.Static);
            if (method == null)
            {
                throw new InvalidOperationException("Could not find the OutputCacheEntryFormatter.GetAsync method.");
            }

            // (string key, IOutputCacheStore store, CancellationToken cancellationToken) 
            var keyParameter = Expression.Parameter(typeof(string));
            var storeParameter = Expression.Parameter(typeof(IOutputCacheStore));
            var cancellationTokenParameter = Expression.Parameter(typeof(CancellationToken));
            // OutputCacheEntryFormatter.GetAsync(key, store, cancellationToken)
            var call = Expression.Call(method, keyParameter, storeParameter, cancellationTokenParameter);
            // var outputCacheEntry = OutputCacheEntryFormatter.GetAsync(key, store, cancellationToken).GetAwaiter().GetResult();
            var await = Expression.Call(call, nameof(ValueTask.GetAwaiter), Type.EmptyTypes);
            var getResult = Expression.Call(await, nameof(ValueTaskAwaiter.GetResult), Type.EmptyTypes);
            var getBody = Expression.MakeMemberAccess(getResult, cachedResponseBodyProperty.Value);
            var lambda = Expression.Lambda<Func<string, IOutputCacheStore, CancellationToken, ReadOnlySequence<byte>>>(getBody, keyParameter, storeParameter, cancellationTokenParameter);
            return lambda.Compile();
        });

        _getResponseBodyFromCacheEntry = CreateLazy(() =>
        {
            var outputCacheEntryType = outputCacheEntryProperty.Value.PropertyType;
            var bodyProperty = cachedResponseBodyProperty.Value;

            var parameter = Expression.Parameter(typeof(object));
            var bodyExpression = Expression.MakeMemberAccess(Expression.Convert(parameter, outputCacheEntryType), bodyProperty);
            var lambda = Expression.Lambda<Func<object, ReadOnlySequence<byte>>>(bodyExpression, parameter);
            return lambda.Compile();
        });
    }

    private static Lazy<T> CreateLazy<T>(Func<T> valueFactory) => new(valueFactory, LazyThreadSafetyMode.PublicationOnly);

    public static string? CacheKey(this OutputCacheContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return GetPrivateCacheKeyProperty(context);
    }

    public static ReadOnlySequence<byte> CachedResponseBody(this OutputCacheContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return _getPrivateCachedResponseBody.Value(context);
    }

    public static ReadOnlySequence<byte> GetCacheResponse(
        string key, 
        IOutputCacheStore store, 
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(store);
        return _getCacheEntry.Value(key, store, cancellationToken);
    }

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = CACHE_KEY)]
    private static extern string? GetPrivateCacheKeyProperty(OutputCacheContext c);
}