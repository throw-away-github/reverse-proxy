using CF.AccessProxy.Config.Options;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

namespace CF.AccessProxy.Routes;

internal class CFAccessRoute: IRouteProvider
{
    private readonly CFAccessOptions _options;
    
    /// <summary>
    /// Adds the CF-Access-Client-Id and CF-Access-Client-Secret headers to the request.
    /// Forwards the same path, headers, and query parameters to the upstream server. (excluding the base cf-access path)
    /// </summary>
    public CFAccessRoute(IOptions<CFAccessOptions> options)
    {
        _options = options.Value;
    }
    
    public RouteConfig Route => BuildRoute();
    
    private RouteConfig BuildRoute()
    {
        var route = new RouteConfig
        {
            RouteId = _options.RouteId,
            ClusterId = "seedbox",
            Match = new RouteMatch()
            {
                Path = "/cf-access/{**catch-all}"
            }
        };

        return route
            .WithTransformPathRemovePrefix("/cf-access")
            .WithTransformRequestHeader("CF-Access-Client-Id", _options.ClientId)
            .WithTransformRequestHeader("CF-Access-Client-Secret", _options.ClientSecret);
    }
}