using Yarp.ReverseProxy.Configuration;

namespace CF.AccessProxy.Proxy.Routes;

public interface IRouteProvider
{
    public RouteConfig Route { get; }
}