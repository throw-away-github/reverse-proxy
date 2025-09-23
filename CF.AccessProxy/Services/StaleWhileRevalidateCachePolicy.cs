using CF.AccessProxy.Config;
using CF.AccessProxy.Extensions;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IO;

namespace CF.AccessProxy.Services;

public sealed class StaleWhileRevalidateCachePolicy : IOutputCachePolicy
{
    private const string REVALIDATE_CACHE_HEADER = "Revalidate-Cache";
    private static readonly RecyclableMemoryStreamManager _streamManager = new();

    private readonly HttpClient _httpClient;
    private readonly ILogger<StaleWhileRevalidateCachePolicy> _logger;
    private readonly CacheOptions _options;
    private readonly WorkerProcessor<string> _workerProcessor;

    public StaleWhileRevalidateCachePolicy(
        IHttpClientFactory httpClientFactory,
        ILogger<StaleWhileRevalidateCachePolicy> logger,
        IOptions<CacheOptions> options,
        WorkerProcessor<string> workerProcessor)
    {
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
        _options = options.Value;
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
        }

        return ValueTask.CompletedTask;
    }

    async ValueTask IOutputCachePolicy.ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        if (!context.EnableOutputCaching || !context.AllowCacheLookup)
        {
            return;
        }

        var cachedEntryAge = context.CachedEntryAge();
        if (cachedEntryAge < _options.StaleExpirationTimeSpan)
        {
            return;
        }

        var cacheKey = context.CacheKey();
        if (string.IsNullOrEmpty(cacheKey) || _workerProcessor.IsEnqueued(cacheKey))
        {
            return;
        }

        var requestCopy = await CopyRequest(context.HttpContext.Request);
        EnqueueWorkItem(cacheKey, requestCopy);
    }

    private void EnqueueWorkItem(string cacheKey, HttpRequestMessage requestCopy)
    {
        var item = (Self: this, Request: requestCopy);
        _workerProcessor.Enqueue(
            cacheKey, 
            item, 
            static (cacheKey, item) => item.Self.Revalidate(cacheKey, item.Request), 
            static item => item.Request.Dispose());
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
            _logger.FailedToRevalidateCacheEntry(cacheKey, response.ReasonPhrase);
        }
    }
}