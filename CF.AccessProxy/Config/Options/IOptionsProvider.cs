using JetBrains.Annotations;

namespace CF.AccessProxy.Config.Options;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature,
    ImplicitUseTargetFlags.WithInheritors | ImplicitUseTargetFlags.Members)]
public interface IOptionsProvider
{
    static abstract string Prefix { get; }
}