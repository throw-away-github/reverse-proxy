using System.Diagnostics.CodeAnalysis;
using CF.AccessProxy.Extensions;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Primitives;
using Microsoft.IO;

namespace CF.AccessProxy.Services;

public sealed class StaleWhileRevalidateCachePolicy : IOutputCachePolicy
{
    private const string REVALIDATE_CACHE_HEADER = "Revalidate-Cache";
    private static readonly RecyclableMemoryStreamManager _streamManager = new();

    private readonly HttpClient _httpClient;
    private readonly ILogger<StaleWhileRevalidateCachePolicy> _logger;
    private readonly WorkerProcessor<string> _workerProcessor;

    public StaleWhileRevalidateCachePolicy(
        HttpClient httpClient,
        ILogger<StaleWhileRevalidateCachePolicy> logger,
        WorkerProcessor<string> workerProcessor)
    {
        _httpClient = httpClient;
        _logger = logger;
        _workerProcessor = workerProcessor;
    }

    ValueTask IOutputCachePolicy.ServeResponseAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
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
        }, static state => state.request.Dispose());
    }

    private static async ValueTask<HttpRequestMessage> CopyRequest(HttpRequest request)
    {
        var url = request.GetEncodedUrl();
        var copyRequest = new HttpRequestMessage(new HttpMethod(request.Method), url);
        var contentLength = request.ContentLength ?? 0;
        var hasContent = contentLength > 0;

        if (hasContent)
        {
            var stream = _streamManager.GetStream(url, contentLength);
            request.EnableBuffering();
            request.Body.Position = 0;
            await request.Body.CopyToAsync(stream);
            stream.Position = 0;
            copyRequest.Content = new StreamContent(stream);
        }

        foreach ((string key, StringValues value) in request.Headers)
        {
            var values = value.ToArray();
            if (!copyRequest.Headers.TryAddWithoutValidation(key, values) && hasContent)
            {
                copyRequest.Content!.Headers.TryAddWithoutValidation(key, values);
            }
        }

        copyRequest.Headers.Add(REVALIDATE_CACHE_HEADER, bool.TrueString);

        return copyRequest;
    }

    private async Task Revalidate(string cacheKey, HttpRequestMessage request)
    {
        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to revalidate cache entry for key {CacheKey}: {Reason}", cacheKey, response.ReasonPhrase);
        }
    }
}