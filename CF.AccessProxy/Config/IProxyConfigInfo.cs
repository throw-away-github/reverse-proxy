using Yarp.ReverseProxy.Configuration;

namespace CF.AccessProxy.Config;

public interface IProxyConfigInfo
{
    IReadOnlyList<RouteConfig> Routes { get; }
    IReadOnlyList<ClusterConfig> Clusters { get; }
}