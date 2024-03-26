using CF.AccessProxy.Models;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace CF.AccessProxy.Proxy.Transforms;

internal abstract class SimpleTransform: ITransform
{
    /// <inheritdoc />
    public bool Validate(TransformRouteValidationContext context, IReadOnlyDictionary<string, string> transformValues)
    {
        return Validate(transformValues);
    }

    private bool Validate(IReadOnlyDictionary<string, string> transformValues)
    {
        return transformValues.ContainsKey(GetType().GUID.ToString()) && ValidateArgs(transformValues);
    }

    /// <inheritdoc />
    public bool Build(TransformBuilderContext context, IReadOnlyDictionary<string, string> transformValues)
    {
        if (!Validate(transformValues))
        {
            return false;
        }
        context.AddRequestTransform(ctx => RequestTransform(new SimpleRequestContext
        {
            Context = ctx,
            Args = transformValues,
            Transform = context
        }));
        context.AddResponseTransform(ctx => ResponseTransform(new SimpleResponseContext
        {
            Context = ctx,
            Args = transformValues,
            Transform = context
        }));
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
    protected abstract ValueTask RequestTransform(SimpleRequestContext context);
    
    /// <summary>
    /// Transform the response
    /// </summary>
    protected abstract ValueTask ResponseTransform(SimpleResponseContext context);
}