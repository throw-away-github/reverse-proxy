using CF.AccessProxy.Extensions;
using CF.AccessProxy.Proxy.Clusters;
using CF.AccessProxy.Proxy.Routes;
using Yarp.ReverseProxy.Configuration;

namespace CF.AccessProxy.Config;

internal class InMemoryConfig : IProxyConfigInfo
{
    public IReadOnlyList<RouteConfig> Routes { get; }
    public IReadOnlyList<ClusterConfig> Clusters { get; }

    /// <summary>
    /// A simple config which creates a list of <see cref="RouteConfig"/> and <see cref="ClusterConfig"/>
    /// from the available <see cref="IRouteProvider"/> and <see cref="IClusterProvider"/> implementations.
    /// </summary>
    public InMemoryConfig(
        IEnumerable<IRouteProvider> routeProviders,
        IEnumerable<IClusterProvider> clusterProviders)
    {
        Routes = routeProviders.SelectMany(provider => provider.Routes).AsList().AsReadOnly();
        Clusters = clusterProviders.SelectMany(provider => provider.Clusters).AsList().AsReadOnly();
    }
}