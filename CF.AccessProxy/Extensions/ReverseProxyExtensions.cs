using CF.AccessProxy.Config;
using CF.AccessProxy.Proxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace CF.AccessProxy.Extensions;

public static class ReverseProxyExtensions
{
    /// <summary>
    /// Loads the routes and clusters from the IProxyConfigInfo service into memory
    /// </summary>
    /// <exception cref="System.InvalidOperationException">There is no IProxyConfigInfo service registered</exception>
    public static IReverseProxyBuilder LoadFromProviders(this IReverseProxyBuilder proxyBuilder)
    {
        using var scope = proxyBuilder.Services.BuildServiceProvider().CreateScope();

        var info = scope.ServiceProvider
            .GetRequiredService<IProxyConfigInfo>();

        proxyBuilder.LoadFromMemory(info.Routes, info.Clusters);

        return proxyBuilder;
    }
    
    public static IReverseProxyBuilder AddAllTransforms(this IReverseProxyBuilder proxyBuilder)
    {
        using var scope = proxyBuilder.Services.BuildServiceProvider().CreateScope();

        var method = typeof(ReverseProxyServiceCollectionExtensions)
            .GetMethod(nameof(ReverseProxyServiceCollectionExtensions.AddTransformFactory));
        ServiceCollectionExtensions.InvokeWithImplementations(method, typeof(ITransform), proxyBuilder);

        return proxyBuilder;
    }
}

