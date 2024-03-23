using System.Text.Json;
using System.Text.Json.Serialization;

namespace CF.AccessProxy.Models;

public class IpNetworkJConverter : JsonConverter<IpNetwork>
{
    public override IpNetwork Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.GetString() is not { } ipNetwork)
            throw new JsonException($"Expected string but got {reader.TokenType}");
        
        if (!IpNetwork.TryParse(ipNetwork, out var result))
            throw new JsonException($"Unable to convert '{ipNetwork}' to IPNetwork");
        
        return result;
    }

    public override void Write(Utf8JsonWriter writer, IpNetwork value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}