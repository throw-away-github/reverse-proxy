using CF.AccessProxy.Clusters;
using CF.AccessProxy.Routes;
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
        Routes = routeProviders.Select(provider => provider.Route).ToList().AsReadOnly();
        Clusters = clusterProviders.Select(provider => provider.Cluster).ToList().AsReadOnly();
    }
}