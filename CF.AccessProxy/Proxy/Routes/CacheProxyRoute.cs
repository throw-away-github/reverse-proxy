using CF.AccessProxy.Config;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

namespace CF.AccessProxy.Proxy.Routes;

internal class CacheProxyRoute : IRouteProvider
{
    private readonly CacheRouteOptions _options;

    /// <summary>
    /// Adds the CF-Access-Client-Id and CF-Access-Client-Secret headers to the request.
    /// Forwards the same path, headers, and query parameters to the upstream server. (excluding the base cf-access path)
    /// </summary>
    public CacheProxyRoute(IOptions<CacheRouteOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// Takes the list of proxies from the config and builds a route for each one.
    /// </summary>
    /// <remarks>
    /// Path /{base path}/{proxy key}
    /// </remarks>
    /// <returns>A list of <see cref="RouteConfig"/> to be used by the proxy.</returns>
    public IEnumerable<RouteConfig> BuildRoutes()
    {
        // The base path is the path that the proxy will be listening on.
        // Always starts with a / even if it is empty.
        var basePath = _options.BasePath;
        if (!Path.IsPathRooted(basePath))
        {
            basePath = $"/{basePath}";
        }

        foreach (var proxyKey in _options.Proxies.Keys)
        {
            // The route path is the path that our proxy will be listening on to forward to the upstream server.
            // It is the base path + the proxy key.
            // If the proxy key is "root" then only the base path is used.
            var routePath = proxyKey.Equals("root", StringComparison.OrdinalIgnoreCase)
                ? basePath
                : Path.Join(basePath, proxyKey);

            var route = new RouteConfig
            {
                RouteId = proxyKey,
                ClusterId = proxyKey,
                Match = new RouteMatch
                {
                    Path = Path.Join(routePath, "{**remainder}")
                }
            };

            if (routePath is not ['/'])
            {
                // remove the route path from the request path that is sent to the upstream server
                // add the route path to the request X-Forwarded-Prefix header so the upstream server can rewrite the response
                route = route
                    .WithTransformPathRemovePrefix(new PathString(routePath))
                    .WithTransformXForwarded(xPrefix: ForwardedTransformActions.Off)
                    .WithTransformRequestHeader("X-Forwarded-Prefix", routePath, append: false); 
            }

            yield return route;
        }
    }
}