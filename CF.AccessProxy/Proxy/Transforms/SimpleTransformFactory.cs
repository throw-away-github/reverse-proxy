using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace CF.AccessProxy.Proxy.Transforms;

internal abstract class SimpleTransformFactory: ITransform
{
    public static string Id { get; } = Guid.NewGuid().ToString();

    public bool Validate(TransformRouteValidationContext context, IReadOnlyDictionary<string, string> transformValues)
    {
        return transformValues.ContainsKey(Id);
    }

    public bool Build(TransformBuilderContext context, IReadOnlyDictionary<string, string> transformValues)
    {
        context.AddRequestTransform(RequestTransform);
        context.AddResponseTransform(ResponseTransform);
        return true;
    }
    
    protected abstract ValueTask RequestTransform(RequestTransformContext ctx);
    protected abstract ValueTask ResponseTransform(ResponseTransformContext ctx);
}