using CF.AccessProxy.Config.Options;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace CF.AccessProxy.Proxy.Transforms;

// ReSharper disable once ClassNeverInstantiated.Global
internal class CFAccessTransform: SimpleTransform
{
    private readonly CFAccessOptions _options;

    public CFAccessTransform(IOptions<CFAccessOptions> options)
    {
        _options = options.Value;
    }

    protected override bool ValidateArgs(IReadOnlyDictionary<string, string> args) => true;

    protected override ValueTask RequestTransform(RequestTransformContext context, IReadOnlyDictionary<string, string> args)
    {
        return default;
    }

    protected override ValueTask ResponseTransform(ResponseTransformContext context, IReadOnlyDictionary<string, string> args)
    {
        // for now, if two set-cookie headers are present, and one is the jwt cookie, we'll remove the other one
        // this is because Radarr has the weirdest cookie behavior I've ever seen and even I couldn't figure it out

        if (!context.HttpContext.Response.Headers.TryGetValue("Set-Cookie", out var cookies))
            return default;
            
        var cookieList = cookies.ToList();
        if (cookieList.Count <= 1)
            return default;
            
        var jwtCookie = cookieList.FirstOrDefault(x => x != null && x.StartsWith("jwt="));
        if (jwtCookie == null)
            return default;
            
        context.HttpContext.Response.Headers["Set-Cookie"] = jwtCookie;
        return default;
    }
}