using CF.AccessProxy.Clusters;
using CF.AccessProxy.Config;
using CF.AccessProxy.Routes;

namespace CF.AccessProxy;

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
}

