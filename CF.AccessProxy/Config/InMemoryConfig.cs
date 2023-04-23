using CF.AccessProxy.Clusters;
using CF.AccessProxy.Routes;
using Yarp.ReverseProxy.Configuration;

namespace CF.AccessProxy.Config;



internal class InMemoryConfig : IProxyConfigInfo
{
    private readonly IEnumerable<IRouteProvider> _routeProviders;
    private readonly IEnumerable<IClusterProvider> _clusterProviders;

    /// <summary>
    /// A simple config which creates a list of <see cref="RouteConfig"/> and <see cref="ClusterConfig"/>
    /// from the available <see cref="IRouteProvider"/> and <see cref="IClusterProvider"/> implementations.
    /// </summary>
    public InMemoryConfig(
        IEnumerable<IRouteProvider> routeProviders, 
        IEnumerable<IClusterProvider> clusterProviders)
    {
        _routeProviders = routeProviders;
        _clusterProviders = clusterProviders;
    }

    public IReadOnlyList<RouteConfig> Routes => 
        _routeProviders.Select(provider => provider.Route).ToList().AsReadOnly();
    
    public IReadOnlyList<ClusterConfig> Clusters => 
        _clusterProviders.Select(provider => provider.Cluster).ToList().AsReadOnly();
}
