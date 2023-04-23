using System.ComponentModel.DataAnnotations;

namespace CF.AccessProxy.Config.Options;

public class CFAccessOptions: IOptionsProvider
{
    public const string CFAccess = "CFAccess";
    [Required]
    public string ClientId { get; set; } = string.Empty;
    [Required]
    public string ClientSecret { get; set; } = string.Empty;

    public static string Prefix => "CFAccess";
}