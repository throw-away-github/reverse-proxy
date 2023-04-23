using Yarp.ReverseProxy.Configuration;

namespace CF.AccessProxy.Routes;

internal interface IRouteProvider
{
    public RouteConfig Route { get; }
}