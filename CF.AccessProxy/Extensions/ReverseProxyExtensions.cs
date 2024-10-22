using CF.AccessProxy.Config;
using Yarp.ReverseProxy.Configuration;

namespace CF.AccessProxy.Extensions;

public static class ReverseProxyExtensions
{
    /// <summary>
    /// Loads the routes and clusters from the IProxyConfigInfo service into memory
    /// </summary>
    /// <exception cref="System.InvalidOperationException">There is no IProxyConfigInfo service registered</exception>
    public static IServiceCollection LoadProxyFromProviders(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryConfigProvider>(provider =>
        {
            var info = provider.GetRequiredService<IProxyConfigInfo>();
            return new InMemoryConfigProvider(info.Routes, info.Clusters);
        });
        services.AddSingleton<IProxyConfigProvider>(provider => provider.GetRequiredService<InMemoryConfigProvider>());
        return services;
    }
}

