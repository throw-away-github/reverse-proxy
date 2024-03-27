using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using CF.AccessProxy.Config.Options;
using CF.AccessProxy.Models;
using CF.AccessProxy.Services;
using Microsoft.Extensions.Options;

namespace CF.AccessProxy.Proxy.Transforms;

internal class CFAccessTransform: SimpleTransform
{
    private readonly CFAccessOptions _options;
    private readonly GithubMetaService _metaService;
    private readonly ILogger<CFAccessTransform> _logger;

    public CFAccessTransform(IOptions<CFAccessOptions> options, GithubMetaService metaService, ILogger<CFAccessTransform> logger)
    {
        _options = options.Value;
        _metaService = metaService;
        _logger = logger;
    }

    protected override bool ValidateArgs(IReadOnlyDictionary<string, string> args) => true;
    
    private static bool TryGetRequestIp(SimpleRequestContext context, [NotNullWhen(true)] out IPAddress? ip)
    {
        var headers = context.HttpContext.Request.Headers;

        if (TryParseHeaderIp(headers, "X-Forwarded-For", out ip))
        {
            return true;
        }
        if (TryParseHeaderIp(headers, "CF-Connecting-IP", out ip))
        {
            return true;
        }
        
        var remoteIp = context.HttpContext.Connection.RemoteIpAddress;
        if (remoteIp != null)
        {
            ip = remoteIp;
            return true;
        }
        
        ip = null;
        return false;
        
        static bool TryParseHeaderIp(
            IHeaderDictionary headers, 
            string headerName, 
            [NotNullWhen(true)] out IPAddress? ip)
        {
            if (headers.TryGetValue(headerName, out var headerValues))
            {
                foreach (var ipString in headerValues)
                {
                    if (!IPAddress.TryParse(ipString, out var ipValue)) continue;
                    ip = ipValue;
                    return true;
                }
            }
            ip = null;
            return false;
        }
    }

    protected override async ValueTask RequestTransform(SimpleRequestContext context)
    {
        if (!TryGetRequestIp(context, out var remoteIp))
        {
            _logger.LogWarning("Failed to get the request IP for {Path}", context.HttpContext.Request.Path);
            return;
        }

        // if the incoming request is from inside the docker network, we'll skip the IP check
        if (remoteIp.AddressFamily is AddressFamily.InterNetworkV6 or AddressFamily.InterNetwork)
        {
            // verify the request is coming from a valid IP
            var isGithubIp = await _metaService.IsGithubIpAsync(remoteIp);
            if (!isGithubIp)
            {
                _logger.LogWarning("Request from {Ip} is not a valid Github IP", remoteIp);
                context.HttpContext.Response.StatusCode = (int) HttpStatusCode.Forbidden;
                return;
            }
        }
        else
        {
            _logger.LogInformation("Request from {Ip} is internal, skipping IP check", remoteIp);
        }
        
        // add the CF-Access-Client-Id header
        context.ProxyRequest.Headers.Add("CF-Access-Client-Id", _options.ClientId);
        context.ProxyRequest.Headers.Add("CF-Access-Client-Secret", _options.ClientSecret);
    }

    protected override ValueTask ResponseTransform(SimpleResponseContext context)
    {
        // for now, if two set-cookie headers are present, and one is the jwt cookie, we'll remove the other one
        // this is because Radarr has the weirdest cookie behavior I've ever seen and even I couldn't figure it out

        if (!context.Http.Response.Headers.TryGetValue("Set-Cookie", out var cookies))
            return default;
            
        var cookieList = cookies.ToList();
        if (cookieList.Count <= 1)
            return default;
            
        var jwtCookie = cookieList.FirstOrDefault(x => x != null && x.StartsWith("jwt="));
        if (jwtCookie == null)
            return default;
            
        context.Http.Response.Headers.SetCookie = jwtCookie;
        return default;
    }
}