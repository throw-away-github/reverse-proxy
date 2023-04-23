using CF.AccessProxy.Config.Options;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Configuration;

namespace CF.AccessProxy.Clusters;

internal class SeedBoxCluster: IClusterProvider
{
    private readonly CFAccessOptions _options;
    
    public SeedBoxCluster(IOptions<CFAccessOptions> options)
    {
        _options = options.Value;
    }
    
    public ClusterConfig Cluster => Create();
    private ClusterConfig Create()
    {
        return new ClusterConfig
        {
            ClusterId = "seedbox",
            Destinations = _options.DestinationConfig
        };
    }
}