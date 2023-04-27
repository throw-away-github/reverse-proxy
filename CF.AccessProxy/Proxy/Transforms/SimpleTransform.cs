using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace CF.AccessProxy.Proxy.Transforms;

internal abstract class SimpleTransform: ITransform
{
    /// <inheritdoc />
    public static string Id { get; } = Guid.NewGuid().ToString();

    /// <inheritdoc />
    public bool Validate(TransformRouteValidationContext context, IReadOnlyDictionary<string, string> transformValues)
    {
        return transformValues.ContainsKey(Id) && ValidateArgs(transformValues);
    }


    /// <inheritdoc />
    public bool Build(TransformBuilderContext context, IReadOnlyDictionary<string, string> transformValues)
    {
        context.AddRequestTransform(ctx => RequestTransform(ctx, transformValues));
        context.AddResponseTransform(ctx => ResponseTransform(ctx, transformValues));
        return true;
    }
    
    /// <summary>
    /// Validate the arguments passed to the transform,
    /// will stop the application from starting if the arguments are invalid
    /// </summary>
    /// <returns>True if the arguments are valid, false otherwise</returns>
    protected abstract bool ValidateArgs(IReadOnlyDictionary<string, string> args);
    
    /// <summary>
    /// Transform the request
    /// </summary>
    protected abstract ValueTask RequestTransform(RequestTransformContext context, IReadOnlyDictionary<string, string> args);
    
    /// <summary>
    /// Transform the response
    /// </summary>
    protected abstract ValueTask ResponseTransform(ResponseTransformContext context, IReadOnlyDictionary<string, string> args);
}