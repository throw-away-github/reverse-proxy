using System.Text.Json;
using CF.AccessProxy.Models;

namespace CF.AccessProxy.Proxy.Transforms;

internal class NugetIndexTransform : SimpleTransform
{
    protected override bool ValidateArgs(IReadOnlyDictionary<string, string> args) => true;

    protected override ValueTask RequestTransform(SimpleRequestContext context)
    {
        return default;
    }

    protected override ValueTask ResponseTransform(SimpleResponseContext context)
    {
        // if filename is not index.json, return
        
        var requestPath = context.Http.Request.Path;
        if (!requestPath.HasValue || !requestPath.Value.EndsWith("index.json"))
        {
            return default;
        }
        
        // if the response is not a 200, return
        if (context.Proxy is not { IsSuccessStatusCode: true })
        {
            return default;
        }
        
        // if the response is not json, return
        var mediaType = context.Proxy.Content.Headers.ContentType?.MediaType;
        if (!mediaType?.Equals("application/json", StringComparison.OrdinalIgnoreCase) == true)
        {
            return default;
        }

        var route = context.Transform.Route.RouteId;
        var destination = context.Transform.Cluster?.Destinations?.Values.FirstOrDefault();
        if (destination == null)
        {
            return default;
        }
        
        var oldUrlPrefix = destination.Address;
        var newUriBuilder = new UriBuilder
        {
            Scheme = context.Http.Request.Scheme,
            Host = context.Http.Request.Host.Host,
            Path = route
        };
        
        var requestPort = context.Http.Request.Host.Port;
        if (requestPort.HasValue)
        {
            newUriBuilder.Port = requestPort.Value;
        }
        
        var newUrlPrefix = newUriBuilder.Uri.ToString();
        
        context.SuppressResponseBody = true;
        return AwaitResponseTransform(context.Proxy, context.Http.Response.Body, oldUrlPrefix, newUrlPrefix);
    }
    
    private static async ValueTask AwaitResponseTransform(HttpResponseMessage response, Stream responseStream, string oldUrlPrefix, string newUrlPrefix)
    {
        var nugetIndex = await response.Content.ReadFromJsonAsync(JsonContext.Default.NugetIndex);
        if (nugetIndex == null)
        {
            return;
        }
        ReplaceUrls(nugetIndex.Resources, oldUrlPrefix, newUrlPrefix);
        await JsonSerializer.SerializeAsync(responseStream, nugetIndex, JsonContext.Default.NugetIndex);
    }
    
    private static void ReplaceUrls(List<Resource> resources, ReadOnlySpan<char> oldUrl, ReadOnlySpan<char> newUrl)
    {
        oldUrl = oldUrl.TrimEnd('/');
        newUrl = newUrl.TrimEnd('/');
        if (oldUrl.Equals(newUrl, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        foreach (var resource in resources)
        {
            resource.Id = ReplaceStart(resource.Id, oldUrl, newUrl);
        }
    }
    
    private static string ReplaceStart(string value, ReadOnlySpan<char> oldValue, ReadOnlySpan<char> newValue)
    {
        var valueSpan = value.AsSpan();
        return valueSpan.StartsWith(oldValue) 
            ? $"{newValue}{valueSpan.Slice(oldValue.Length)}"
            : value;
    }
}