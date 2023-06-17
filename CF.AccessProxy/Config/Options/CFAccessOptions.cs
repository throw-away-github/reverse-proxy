using System.ComponentModel.DataAnnotations;
using CF.AccessProxy.Config.Validation;
using Yarp.ReverseProxy.Configuration;

namespace CF.AccessProxy.Config.Options;

internal class CFAccessOptions : IOptionsProvider
{
    public static string Prefix => "CFAccess";

    [Required] public string BasePath { get; init; } = "cf-access";
    [Required] public required string ClientId { get; init; }
    [Required] public required string ClientSecret { get; init; }


    [Required(ErrorMessage = "At least one CFAccess domain needs to be provided.")]
    public required Dictionary<string, Uri> Domains { get; init; }

    internal Lazy<Dictionary<string, DestinationConfig>> DestinationConfig =>
        new(() => DestinationHelper.ConvertToDestinations(Domains));
}