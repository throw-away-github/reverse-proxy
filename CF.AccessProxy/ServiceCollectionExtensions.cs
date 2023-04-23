using System.Reflection;
using CF.AccessProxy.Config.Options;
using Microsoft.Extensions.Options;

namespace CF.AccessProxy;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection LoadOptions<TOptions>(this IServiceCollection services) where TOptions : class, IOptionsProvider
    {
        services.AddOptions<TOptions>()
            .Bind(services
                .BuildServiceProvider()
                .GetRequiredService<IConfiguration>()
                .GetSection(TOptions.Prefix));
        return services;
    }
    
    public static IServiceCollection LoadOptions(this IServiceCollection services)
    {
        var typesFromAssemblies = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(x => x is { IsClass: true, IsAbstract: false } && x.GetInterfaces().Contains(typeof(IOptionsProvider)));

        foreach (var type in typesFromAssemblies)
        {
            // this is a bit of a hack, I'm not sure how to do this without reflection
            var method = typeof(ServiceCollectionExtensions).GetMethod(nameof(LoadOptions));
            var generic = method?.MakeGenericMethod(type);
            generic?.Invoke(null, new object[] { services });
        }

        return services;
    }

    public static void RegisterAllTypes<T>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        var typesFromAssemblies = Assembly.GetExecutingAssembly()
            .DefinedTypes
            .Where(x => x.GetInterfaces().Contains(typeof(T)));
        
        foreach (var type in typesFromAssemblies)
            services.Add(new ServiceDescriptor(typeof(T), type, lifetime));
    }
    
}