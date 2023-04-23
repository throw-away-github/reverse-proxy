using Yarp.ReverseProxy.Configuration;

namespace CF.AccessProxy.Clusters;

public interface IClusterProvider
{
    public ClusterConfig Cluster { get; }
}