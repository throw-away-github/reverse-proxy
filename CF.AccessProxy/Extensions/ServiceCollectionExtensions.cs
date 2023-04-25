using System.Reflection;
using CF.AccessProxy.Config.Options;

namespace CF.AccessProxy.Extensions;

internal static class ServiceCollectionExtensions
{
    /// <summary>
    /// Loads a single implementation of IOptionsProvider, and binds it to the IConfiguration
    /// </summary>
    public static IServiceCollection LoadOptions<TOptions>(this IServiceCollection services)
        where TOptions : class, IOptionsProvider
    {
        services.AddOptions<TOptions>()
            .Bind(services
                .BuildServiceProvider()
                .GetRequiredService<IConfiguration>()
                .GetSection(TOptions.Prefix))
            .ValidateDataAnnotations();
        return services;
    }

    /// <summary>
    /// Loads all implementations of IOptionsProvider, and binds them to the IConfiguration
    /// </summary>
    public static IServiceCollection LoadProviderOptions(this IServiceCollection services)
    {
        var typesFromAssemblies = GetInterfaceImplementations(typeof(IOptionsProvider));

        foreach (var type in typesFromAssemblies)
        {
            // this is a bit of a hack, I hate it, but it works
            // it's still better than having to manually register each option
            var method = typeof(ServiceCollectionExtensions).GetMethod(nameof(LoadOptions));
            var generic = method?.MakeGenericMethod(type);
            generic?.Invoke(null, new object[] { services });
        }

        return services;
    }

    /// <summary>
    /// Registers all implementations of an interface as a service
    /// </summary>
    /// <typeparam name="T">The interface to register</typeparam>
    public static IServiceCollection RegisterAllTypes<T>(this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        var typesFromAssemblies = GetInterfaceImplementations(typeof(T));

        foreach (var type in typesFromAssemblies)
            services.Add(new ServiceDescriptor(typeof(T), type, lifetime));

        return services;
    }

    /// <summary>
    /// Gets all implementations of an interface in the current assembly
    /// </summary>
    private static IEnumerable<Type> GetInterfaceImplementations(Type type)
    {
        return Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(x => x is { IsClass: true, IsAbstract: false } && x.GetInterfaces().Contains(type));
    }
}