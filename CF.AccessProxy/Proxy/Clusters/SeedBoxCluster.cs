using CF.AccessProxy.Config.Options;
using CF.AccessProxy.Extensions;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Configuration;

namespace CF.AccessProxy.Proxy.Clusters;

internal class SeedBoxCluster: IClusterProvider
{
    private readonly CFAccessOptions _options;
    
    public SeedBoxCluster(IOptions<CFAccessOptions> options)
    {
        _options = options.Value;
    }
    
    public IEnumerable<ClusterConfig> Cluster => BuildClusters();
    
    /// <summary>
    /// Takes the list of domains from the config and builds a cluster for each one.
    /// </summary>
    /// <returns>A list of <see cref="ClusterConfig"/> to be used by the proxy.</returns>
    private IEnumerable<ClusterConfig> BuildClusters()
    {
        return _options.Domains.Select(proxy => new ClusterConfig
        {
            ClusterId = proxy.Key,
            Destinations = proxy.ToDictionary(pair => pair.Key, pair => new DestinationConfig
            {
                Address = pair.Value.OriginalString
            })
        });
    }
}