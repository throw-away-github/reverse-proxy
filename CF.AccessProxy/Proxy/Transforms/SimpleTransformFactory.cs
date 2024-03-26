using Yarp.ReverseProxy.Transforms.Builder;

namespace CF.AccessProxy.Proxy.Transforms;

public class SimpleTransformFactory: ITransformFactory
{
    private readonly IEnumerable<ITransform> _transforms;
    
    public SimpleTransformFactory(IEnumerable<ITransform> transforms)
    {
        _transforms = transforms;
    }


    public bool Validate(TransformRouteValidationContext context, IReadOnlyDictionary<string, string> transformValues)
    {
        return _transforms.Any(transform => transform.Validate(context, transformValues));
    }

    public bool Build(TransformBuilderContext context, IReadOnlyDictionary<string, string> transformValues)
    {
        var built = false;
        foreach (var transform in _transforms)
        {
            built |= transform.Build(context, transformValues);
        }
        return built;
    }
}