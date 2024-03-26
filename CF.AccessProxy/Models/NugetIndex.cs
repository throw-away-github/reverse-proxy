using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace CF.AccessProxy.Models;

public record NugetIndex(string Version, List<Resource> Resources);

public record Resource
{
    [SetsRequiredMembers]
    public Resource(string id, string type, string comment)
    {
        Id = id;
        Type = type;
        Comment = comment;
    }

    [JsonPropertyName("@id")]
    public required string Id { get; set; }
    
    [JsonPropertyName("@type")]
    public required string Type { get; init; }
    public required string Comment { get; init; }
}