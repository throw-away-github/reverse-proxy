using Yarp.ReverseProxy.Configuration;

namespace CF.AccessProxy.Clusters;

public class SeedBoxCluster: IClusterProvider
{
    public ClusterConfig Cluster => BuildConfig();
    private static ClusterConfig BuildConfig()
    {
        return new ClusterConfig
        {
            ClusterId = "seedbox",
            Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "dflood", new DestinationConfig
                    {
                        Address = "https://dflood.tcbrooks.com",
                    }
                },
            }
        };
    }
}