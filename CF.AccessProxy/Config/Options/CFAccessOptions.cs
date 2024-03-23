using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace CF.AccessProxy.Config.Options;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature,
    ImplicitUseTargetFlags.WithInheritors | ImplicitUseTargetFlags.Members)]
internal sealed class CFAccessOptions
{
    public static string Prefix => "CFAccess";

    [Required (AllowEmptyStrings = true)] public string BasePath { get; init; } = "cf-access";
    [Required] public required string ClientId { get; init; }
    [Required] public required string ClientSecret { get; init; }

    [Required(ErrorMessage = "At least one CFAccess proxy needs to be provided.")]
    public required Dictionary<string, Uri> Proxies { get; init; }
}

[OptionsValidator]
internal sealed partial class CFAccessValidator : IValidateOptions<CFAccessOptions>;