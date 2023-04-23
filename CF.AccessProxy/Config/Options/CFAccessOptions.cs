using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using CF.AccessProxy.Config.Validation;
using Yarp.ReverseProxy.Configuration;

namespace CF.AccessProxy.Config.Options;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
internal class CFAccessOptions: IOptionsProvider
{
    public static string Prefix => "CFAccess";
    
    [Required] public string ClientId { get; set; } = string.Empty;
    [Required] public string ClientSecret { get; set; } = string.Empty;
    
    [SemicolonSeparatedUrls] public string Domain { get; set; } = string.Empty;
    
    internal Dictionary<string, DestinationConfig> DestinationConfig => 
        DestinationHelper.ConvertStringToDestinations(Domain);
}