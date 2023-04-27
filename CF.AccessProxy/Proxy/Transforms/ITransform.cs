using Yarp.ReverseProxy.Transforms.Builder;

namespace CF.AccessProxy.Proxy.Transforms;

public interface ITransform: ITransformFactory
{
    static abstract string Id { get; }
}
