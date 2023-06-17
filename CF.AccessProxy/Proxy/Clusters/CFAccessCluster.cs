using CF.AccessProxy.Config.Options;
using CF.AccessProxy.Extensions;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Configuration;

namespace CF.AccessProxy.Proxy.Clusters;

internal class CFAccessCluster: IClusterProvider
{
    private readonly CFAccessOptions _options;
    
    public CFAccessCluster(IOptions<CFAccessOptions> options)
    {
        _options = options.Value;
    }
    
    public IEnumerable<ClusterConfig> Clusters => BuildClusters();
    
    /// <summary>
    /// Takes the list of proxies from the config and builds a cluster for each one.
    /// </summary>
    /// <returns>A list of <see cref="ClusterConfig"/> to be used by the proxy.</returns>
    private IEnumerable<ClusterConfig> BuildClusters()
    {
        return _options.Proxies.Select(proxy => new ClusterConfig
        {
            ClusterId = proxy.Key,
            Destinations = proxy.ToDictionary(pair => pair.Key, pair => new DestinationConfig
            {
                Address = pair.Value.OriginalString
            })
        });
    }
}