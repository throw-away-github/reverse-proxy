using CF.AccessProxy.Clusters;
using CF.AccessProxy.Config;
using CF.AccessProxy.Routes;

namespace CF.AccessProxy;

public static class ReverseProxyExtensions
{
    /// <summary>
    /// Loads the routes and clusters from the IProxyConfigInfo service
    /// </summary>
    /// <exception cref="System.InvalidOperationException">There is no IProxyConfigInfo service registered</exception>
    public static IReverseProxyBuilder LoadFromProviders(this IReverseProxyBuilder builder)
    {
        using var scope = builder.Services.BuildServiceProvider().CreateScope();

        var info = scope.ServiceProvider
            .GetRequiredService<IProxyConfigInfo>();

        builder.LoadFromMemory(info.Routes, info.Clusters);

        return builder;
    }
}

