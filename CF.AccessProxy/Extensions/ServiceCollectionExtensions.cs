using System.Reflection;
using CF.AccessProxy.Config.Options;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using DynAccess = System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute;
using static System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;

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
    
    public static OptionsBuilder<TOptions> AddOptions<
        TOptions,
        [DynAccess(PublicConstructors)] TValidator>(this IServiceCollection services, string? name = null)
        where TOptions : class
        where TValidator : class, IValidateOptions<TOptions>
    {
        name ??= Options.DefaultName;
        return services.AddOptions<TOptions>(name)
            .Validate<TOptions, TValidator>();
    }
    
    internal static OptionsBuilder<TOptions> PostConfigure<TOptions, [DynAccess(PublicConstructors)] TConfig>(
        this OptionsBuilder<TOptions> builder)
        where TOptions : class
        where TConfig : class, IPostConfigureOptions<TOptions>
    {
        builder.Services.TryAddSingleton<IPostConfigureOptions<TOptions>, TConfig>();
        return builder;
    }
    
    internal static OptionsBuilder<TOptions> Configure<TOptions, [DynAccess(PublicConstructors)] TConfig>(
        this OptionsBuilder<TOptions> builder)
        where TOptions : class
        where TConfig : class, IConfigureOptions<TOptions>
    {
        builder.Services.TryAddSingleton<IConfigureOptions<TOptions>, TConfig>();
        return builder;
    }
    
    internal static OptionsBuilder<TOptions> Validate<TOptions, [DynAccess(PublicConstructors)] TValidator>(
        this OptionsBuilder<TOptions> builder, bool validateOnStartup = false)
        where TOptions : class
        where TValidator : class, IValidateOptions<TOptions>
    {
        builder.Services.TryAddSingleton<IValidateOptions<TOptions>, TValidator>();
        return validateOnStartup ? builder.ValidateOnStart() : builder;
    }
    
    public static IHttpClientBuilder ConfigureHttpClientUsing<TService>(
        this IHttpClientBuilder builder,
        Action<TService, HttpClient> configureClient)
        where TService : notnull
    {
        return builder.ConfigureHttpClient((provider, client) =>
        {
            var service = provider.GetRequiredService<TService>();
            configureClient.Invoke(service, client);
        });
    }

    /// <summary>
    /// Registers all implementations of an interface as a service
    /// </summary>
    /// <typeparam name="T">The interface to register</typeparam>
    public static IServiceCollection RegisterAllTypes<T>(this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        var typesFromAssemblies = GetInterfaceImplementations(typeof(T));

        foreach (var type in typesFromAssemblies)
            services.Add(new ServiceDescriptor(typeof(T), type, lifetime));

        return services;
    }
    
    public static void InvokeWithImplementations(MethodInfo? method, Type type, params object[] args)
    {
        foreach (var impl in GetInterfaceImplementations(type))
        {
            var generic = method?.MakeGenericMethod(impl);
            generic?.Invoke(null, args);
        }
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