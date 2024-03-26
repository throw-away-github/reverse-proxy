using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using CF.AccessProxy.Models;
using Yarp.ReverseProxy.Transforms.Builder;

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
        if (!requestPath.HasValue || !requestPath.Value.Contains("v3", StringComparison.OrdinalIgnoreCase))
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
        
        if (!UrlReplacementRequest.TryCreate(context, out var urlReplacement))
        {
            return default;
        }

        context.SuppressResponseBody = true;
        return ReplaceUrls(urlReplacement);
    }
    
    private record UrlReplacementRequest
    {
        public required HttpResponseMessage Response { get; init; }
        public required TransformBuilderContext Transform { get; init; }
        public required string OldUrl { get; init; }
        public required string NewUrl { get; init; }
        public required Stream ResponseStream { get; init; }
        
        public static bool TryCreate(
            SimpleResponseContext context, 
            [NotNullWhen(true)] out UrlReplacementRequest? result)
        {
            var transform = context.Transform;
            var proxy = context.Proxy;
            
            var route = transform.Route.RouteId;
            var destination = transform.Cluster?.Destinations?.Values.FirstOrDefault();
            if (destination == null || proxy == null)
            {
                result = null;
                return false;
            }
        
            var oldUrlPrefix = destination.Address;
            var oldUrlScheme = oldUrlPrefix.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? "https"
                : "http";
            
            var request = context.Http.Request;
            var newUriBuilder = new UriBuilder
            {
                Scheme = oldUrlScheme,
                Host = request.Host.Host,
                Path = route
            };
        
            var requestPort = request.Host.Port;
            if (requestPort.HasValue)
            {
                newUriBuilder.Port = requestPort.Value;
            }

            result = new UrlReplacementRequest
            {
                Response = proxy,
                Transform = transform,
                OldUrl = oldUrlPrefix,
                NewUrl = newUriBuilder.Uri.ToString(),
                ResponseStream = context.Http.Response.Body
            };
            return true;
        }
    }
    
    private static async ValueTask ReplaceUrls(UrlReplacementRequest request)
    {
        // load the json into a utf8 json reader and modify any strings that match the old url
        using var json = await JsonDocument.ParseAsync(await request.Response.Content.ReadAsStreamAsync());
        await using var writer = new Utf8JsonWriter(request.ResponseStream);
        
        ReplaceUrls(json.RootElement, writer, request);
    }
    
    private static void ReplaceUrls(JsonProperty property, Utf8JsonWriter writer, UrlReplacementRequest request)
    {
        switch (property.Value.ValueKind)
        {
            case JsonValueKind.String:
                var value = property.Value.GetString();
                if (value == null)
                {
                    writer.WriteNull(property.Name);
                    break;
                }
                var newValue = ReplaceUrl(value, request.OldUrl, request.NewUrl);
                writer.WriteString(property.Name, newValue);
                break;
            case JsonValueKind.Object:
                writer.WriteStartObject(property.Name);
                foreach (var childProperty in property.Value.EnumerateObject())
                {
                    ReplaceUrls(childProperty, writer, request);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray(property.Name);
                foreach (var element in property.Value.EnumerateArray())
                {
                    ReplaceUrls(element, writer, request);
                }
                writer.WriteEndArray();
                break;
            case JsonValueKind.Undefined:
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
            default:
                property.WriteTo(writer);
                break;
        }
    }
    
    private static void ReplaceUrls(JsonElement element, Utf8JsonWriter writer, UrlReplacementRequest request)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                var value = element.GetString();
                if (value == null)
                {
                    writer.WriteNullValue();
                    break;
                }
                var newValue = ReplaceUrl(value, request.OldUrl, request.NewUrl);
                writer.WriteStringValue(newValue);
                break;
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var jsonProperty in element.EnumerateObject())
                {
                    ReplaceUrls(jsonProperty, writer, request);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var jsonElement in element.EnumerateArray())
                {
                    ReplaceUrls(jsonElement, writer, request);
                }
                writer.WriteEndArray();
                break;
            case JsonValueKind.Undefined:
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
            default:
                element.WriteTo(writer);
                break;
        }
    }
    
    private static string ReplaceUrl(string value, scoped ReadOnlySpan<char> oldValue, scoped ReadOnlySpan<char> newValue)
    {
        var valueSpan = value.AsSpan();
        return valueSpan.StartsWith(oldValue) 
            ? $"{newValue}{valueSpan.Slice(oldValue.Length)}"
            : value;
    }
}