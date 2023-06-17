using System.ComponentModel.DataAnnotations;

namespace CF.AccessProxy.Config.Options;

internal class CFAccessOptions : IOptionsProvider
{
    public static string Prefix => "CFAccess";

    [Required] public string BasePath { get; init; } = "cf-access";
    [Required] public required string ClientId { get; init; }
    [Required] public required string ClientSecret { get; init; }


    [Required(ErrorMessage = "At least one CFAccess proxy needs to be provided.")]
    public required Dictionary<string, Uri> Proxies { get; init; }
}