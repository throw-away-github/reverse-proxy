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

    public IEnumerable<RouteConfig> Routes => BuildRoutes();

    /// <summary>
    /// Takes the list of proxies from the config and builds a route for each one.
    /// </summary>
    /// <remarks>
    /// Path /{base path}/{proxy key}
    /// </remarks>
    /// <returns>A list of <see cref="RouteConfig"/> to be used by the proxy.</returns>
    private IEnumerable<RouteConfig> BuildRoutes()
    {
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach (var proxyKey in _options.Proxies.Keys)
        {
            var basePath = Path.IsPathRooted(_options.BasePath) ? _options.BasePath : $"/{_options.BasePath}";
            var route = new RouteConfig
            {
                RouteId = proxyKey,
                ClusterId = proxyKey,
                Match = new RouteMatch
                {
                    Path = Path.Join(_options.BasePath, proxyKey, "{**catch-all}")
                }
            };

            route = route.WithTransformPathRemovePrefix(Path.Join(basePath, route.RouteId));

            yield return route;
        }
    }
}