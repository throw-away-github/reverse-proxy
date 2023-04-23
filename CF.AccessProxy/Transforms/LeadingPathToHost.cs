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
        // If not, at least I could create a custom route provider with a TransformContext property, that would need an abstract class though.
        if (context.Route.RouteId != _options.RouteId)
        {
            Console.WriteLine($"RouteId: {context.Route.RouteId} != {_options.RouteId}");
            return;
        }
        
        context.AddRequestTransform(transformContext =>
        {
            var paths = transformContext.Path.Value?.Split('/');
            var subdomain = paths is { Length: > 1 } ? paths[1] : null;
            
            // I'll probably just create a route and cluster for each subdomain in the future (Route/Cluster Factory?)
            // But for now I'll use this, it's good to have an example of the transform api.
            if (subdomain == null || !_options.DestinationConfig.TryGetValue(subdomain, out var destination))
            {
                // the host needs to be set explicitly, otherwise it seems to be chosen randomly
                // I should try to figure out how that works
                transformContext.ProxyRequest.Headers.Host = 
                    new Uri(_options.DestinationConfig.First().Value.Address).Host;
                return default;
            }
            // set the proxy request host to the destination host
            transformContext.ProxyRequest.Headers.Host = new Uri(destination.Address).Host;
            // remove the subdomain from the path
            transformContext.Path = transformContext.Path.Value?.Replace($"/{subdomain}", "");
            return default;
        });
    }
}