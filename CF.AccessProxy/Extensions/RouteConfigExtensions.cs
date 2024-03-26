using CF.AccessProxy.Proxy.Transforms;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;


namespace CF.AccessProxy.Extensions;

public static class RouteConfigExtensions
{
    public static RouteConfig WithTransformFactory<TTransform>(this RouteConfig route, IDictionary<string, string>? args = null) 
        where TTransform : ITransform
    {
        return route.WithTransform(transform =>
        {
            transform.Add(typeof(TTransform).GUID.ToString(), string.Empty);
            if (args == null) 
                return;
            foreach (var arg in args)
            {
                transform.Add(arg.Key, arg.Value);
            }
        });
    }
}