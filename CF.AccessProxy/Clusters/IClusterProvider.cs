using Yarp.ReverseProxy.Configuration;

namespace CF.AccessProxy.Clusters;

internal interface IClusterProvider
{
    public ClusterConfig Cluster { get; }
}