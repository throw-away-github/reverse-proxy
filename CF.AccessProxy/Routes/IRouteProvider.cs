using Yarp.ReverseProxy.Configuration;

namespace CF.AccessProxy.Routes;

public interface IRouteProvider
{
    public RouteConfig Route { get; }
}