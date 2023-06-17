using Yarp.ReverseProxy.Configuration;

namespace CF.AccessProxy.Proxy.Routes;

public interface IRouteProvider
{
    public IEnumerable<RouteConfig> Routes { get; }
}