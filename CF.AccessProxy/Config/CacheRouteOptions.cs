using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace CF.AccessProxy.Config;

[UsedImplicitly(
    useKindFlags: ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature,
    targetFlags: ImplicitUseTargetFlags.WithInheritors | ImplicitUseTargetFlags.Members)]
internal sealed class CacheRouteOptions
{
    public static string Prefix => "Cache";

    [Required(AllowEmptyStrings = true)] 
    public string BasePath { get; init; } = "";

    [Required(ErrorMessage = "At least one CFAccess proxy needs to be provided.")]
    public required Dictionary<string, Uri> Proxies { get; init; }
}

[OptionsValidator]
internal sealed partial class RouteOptionsValidator : IValidateOptions<CacheRouteOptions>;