using JetBrains.Annotations;
using Yarp.ReverseProxy.Transforms.Builder;

namespace CF.AccessProxy.Proxy.Transforms;

/// <summary>
/// Yarp's ITransformFactory interface, with a static Id property so it can be added without string literals
/// <example><see cref="SimpleTransform"/></example>
/// <seealso cref="Extensions.RouteConfigExtensions"/>
/// </summary>
/// <note type="implement">
/// Any class implementing this interface will be added to the DI container and can be used in the config
/// <see cref="Extensions.ReverseProxyExtensions.AddAllTransforms"/>
/// </note>
[UsedImplicitly (ImplicitUseTargetFlags.WithInheritors)]
public interface ITransform: ITransformFactory;
