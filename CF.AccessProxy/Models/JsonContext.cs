using System.Text.Json.Serialization;

namespace CF.AccessProxy.Models;

/// <summary>
///     Source generated json context for the models in this project.
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    Converters = [typeof(IpNetworkJConverter)],
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    GenerationMode = JsonSourceGenerationMode.Default,
    PropertyNameCaseInsensitive = true,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(GithubMeta), TypeInfoPropertyName = "GithubMeta")]
[JsonSerializable(typeof(IReadOnlyList<IpNetwork>), TypeInfoPropertyName = "IPNetworkSet")]
public partial class JsonContext : JsonSerializerContext;