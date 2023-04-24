using CF.AccessProxy.Config.Options;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace CF.AccessProxy.Transforms;

internal class LeadingPathToHost: ITransformProvider
{
    private readonly CFAccessOptions _options;

    public LeadingPathToHost(IOptions<CFAccessOptions> options)
    {
        _options = options.Value;
    }

    public void ValidateRoute(TransformRouteValidationContext context)
    {
    }

    public void ValidateCluster(TransformClusterValidationContext context)
    {
    }

    /// <summary>
    /// This is highly specific to my use case where I have all my addresses on the same domain, but different subdomains.
    /// Every request to cf-access/{subdomain}/* where the subdomain exists in the config will be routed to the destination address.
    /// </summary>
    /// <param name="context"></param>
    public void Apply(TransformBuilderContext context)
    {
        // I feel like there should be a way to access transform context within the route... Need to look through the source code.
        // I think I could just use the TransformBuilderContext directly, it has a Route property.
        // If I add a dictionary of routeId to IRouteProvider, and added an Apply method to IRouteProvider,
        // I could just look up the routeId in the dictionary and call Apply on the IRouteProvider.
        if (context.Route.RouteId != _options.RouteId)
        {
            return;
        }

        context.AddRequestTransform(ctx =>
        {
            var paths = ctx.Path.Value?.Split('/');
            var subdomain = paths is { Length: > 1 } ? paths[1] : null;
            
            // I'll probably just create a route and cluster for each subdomain in the future (Route/Cluster Factory?)
            // But for now I'll use this, it's good to have an example of the transform api.
            if (subdomain == null || !_options.DestinationConfig.Value.TryGetValue(subdomain, out var destination))
            {
                // the host needs to be set explicitly, otherwise it seems to be chosen randomly
                // I should try to figure out how that works
                ctx.ProxyRequest.Headers.Host = 
                    new Uri(_options.DestinationConfig.Value.First().Value.Address).Host;
                return default;
            }
            // set the proxy request host to the destination host
            ctx.ProxyRequest.Headers.Host = new Uri(destination.Address).Host;
            // remove the subdomain from the path
            ctx.Path = ctx.Path.Value?.Replace($"/{subdomain}", "");
            return default;
        });
        
        context.AddResponseTransform(ctx =>
        {
            // for now, if two set-cookie headers are present, and one is the jwt cookie, we'll remove the other one
            // this is because Radarr has the weirdest cookie behavior I've ever seen and even I couldn't figure it out

            if (!ctx.HttpContext.Response.Headers.TryGetValue("Set-Cookie", out var cookies))
                return default;
            
            var cookieList = cookies.ToList();
            if (cookieList.Count <= 1)
                return default;
            
            var jwtCookie = cookieList.FirstOrDefault(x => x != null && x.StartsWith("jwt="));
            if (jwtCookie == null)
                return default;
            
            ctx.HttpContext.Response.Headers["Set-Cookie"] = jwtCookie;
            
            return default;
        });
    }
}