using JetBrains.Annotations;
using Yarp.ReverseProxy.Configuration;

namespace CF.AccessProxy.Proxy.Routes;

[UsedImplicitly (ImplicitUseTargetFlags.WithInheritors)]
public interface IRouteProvider
{
    public IEnumerable<RouteConfig> Routes { get; }
}