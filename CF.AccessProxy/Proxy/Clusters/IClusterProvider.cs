using JetBrains.Annotations;
using Yarp.ReverseProxy.Configuration;

namespace CF.AccessProxy.Proxy.Clusters;

[UsedImplicitly (ImplicitUseTargetFlags.WithInheritors)]
public interface IClusterProvider
{
    public IEnumerable<ClusterConfig> Clusters { get; }
}