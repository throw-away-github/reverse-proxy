using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace CF.AccessProxy.Models;

public record SimpleContext<T>
{
    public required T Context { get; init; }
    public required IReadOnlyDictionary<string, string> Args { get; init; }
    public required TransformBuilderContext Transform { get; init; }
}

public record SimpleRequestContext : SimpleContext<RequestTransformContext>
{
    /// <inheritdoc cref="RequestTransformContext.HttpContext"/>
    public HttpContext HttpContext => Context.HttpContext;
    
    /// <inheritdoc cref="RequestTransformContext.ProxyRequest"/>
    public HttpRequestMessage ProxyRequest => Context.ProxyRequest;
}

public record SimpleResponseContext : SimpleContext<ResponseTransformContext>
{
    /// <inheritdoc cref="ResponseTransformContext.HttpContext"/>
    public HttpContext Http => Context.HttpContext;
    
    /// <inheritdoc cref="ResponseTransformContext.ProxyResponse"/>
    public HttpResponseMessage? Proxy => Context.ProxyResponse;

    /// <inheritdoc cref="ResponseTransformContext.SuppressResponseBody"/>
    public bool SuppressResponseBody
    {
        get => Context.SuppressResponseBody; 
        set => Context.SuppressResponseBody = value;
    }
}