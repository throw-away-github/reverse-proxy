using Yarp.ReverseProxy.Configuration;

namespace CF.AccessProxy.Proxy.Clusters;

public interface IClusterProvider
{
    public ClusterConfig Cluster { get; }
}