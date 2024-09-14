using CF.AccessProxy.Extensions;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Primitives;

namespace CF.AccessProxy.Services;

public sealed class StaleWhileRevalidateCachePolicy : IOutputCachePolicy
{
    private const string REVALIDATE_CACHE_HEADER = "Revalidate-Cache";
    private readonly HttpClient _httpClient;
    private readonly ILogger<StaleWhileRevalidateCachePolicy> _logger;
    private readonly IOutputCacheStore _cacheStore;
    private readonly WorkerProcessor<string> _workerProcessor;

    public StaleWhileRevalidateCachePolicy(
        HttpClient httpClient, 
        IOutputCacheStore cacheStore,
        ILogger<StaleWhileRevalidateCachePolicy> logger,
        WorkerProcessor<string> workerProcessor)
    {
        _httpClient = httpClient;
        _cacheStore = cacheStore;
        _logger = logger;
        _workerProcessor = workerProcessor;
    }

    ValueTask IOutputCachePolicy.CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        if (context.HttpContext.Request.Headers.ContainsKey(REVALIDATE_CACHE_HEADER))
        {
            context.AllowCacheLookup = false;
            return ValueTask.CompletedTask;
        }
        return ValueTask.CompletedTask;
    }

    async ValueTask IOutputCachePolicy.ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        if (!context.EnableOutputCaching || !context.AllowCacheLookup)
        {
            return;
        }

        var cacheKey = context.CacheKey();
        if (string.IsNullOrEmpty(cacheKey) || _workerProcessor.IsEnqueued(cacheKey))
        {
            return;
        }

        var request = await CopyRequest(context.HttpContext.Request);
        _workerProcessor.Enqueue(cacheKey, (this, request), static (key, state) =>
        {
            var (policy, request) = state;
            return policy.Revalidate(key, request);
        });
    }

    private static async ValueTask<HttpRequestMessage> CopyRequest(HttpRequest request)
    {
        var requestMessage = new HttpRequestMessage(new HttpMethod(request.Method), request.GetEncodedUrl());
        var hasContent = request.ContentLength is > 0;
        if (hasContent)
        {
            request.EnableBuffering();
            request.Body.Position = 0;
            using var streamReader = new StreamReader(request.Body);
            requestMessage.Content = new StringContent(await streamReader.ReadToEndAsync());
        }

        foreach ((string key, StringValues value) in request.Headers)
        {
            var values = value.ToArray();
            if (!requestMessage.Headers.TryAddWithoutValidation(key, values) && hasContent)
            {
                requestMessage.Content!.Headers.TryAddWithoutValidation(key, values);
            }
        }

        requestMessage.Headers.Add(REVALIDATE_CACHE_HEADER, bool.TrueString);

        return requestMessage;
    }

    private async Task Revalidate(string cacheKey, HttpRequestMessage request)
    {
        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to revalidate cache entry for key {CacheKey}: {Reason}", cacheKey, response.ReasonPhrase);
            await _cacheStore.EvictByTagAsync(cacheKey, CancellationToken.None);
        }
    }

    ValueTask IOutputCachePolicy.ServeResponseAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}