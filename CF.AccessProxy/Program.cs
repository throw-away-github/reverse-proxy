using CF.AccessProxy.Config;
using CF.AccessProxy.Extensions;
using CF.AccessProxy.Proxy.Clusters;
using CF.AccessProxy.Proxy.Routes;
using CF.AccessProxy.Services;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Load options, providers, and config
builder.Services
    .AddOptions<CacheRouteOptions, RouteOptionsValidator>()
    .BindConfiguration(CacheRouteOptions.Prefix)
    .ValidateOnStart();

builder.Services
    .RegisterAllTypes<IClusterProvider>()
    .RegisterAllTypes<IRouteProvider>()
    .AddSingleton<IProxyConfigInfo, InMemoryConfig>()
    .LoadProxyFromProviders();

// Add Output Cache
builder.Services.AddHttpClient();
builder.Services.AddSingleton<WorkerProcessor<string>>();
builder.Services.AddOutputCache(static options =>
{
    options.AddBasePolicy(policy =>
    {
        policy.SetVaryByHost(true);
        policy.AddPolicy<StaleWhileRevalidateCachePolicy>();
    });
    options.DefaultExpirationTimeSpan = TimeSpan.FromMinutes(5);
});

builder.Services.Configure<CacheOptions>(null, builder.Configuration.GetSection(CacheRouteOptions.Prefix));
builder.Services.Configure<OutputCacheOptions>(null, builder.Configuration.GetSection(CacheRouteOptions.Prefix));

// Add Http Logging
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.RequestPath
                            | HttpLoggingFields.ResponseStatusCode
                            | HttpLoggingFields.RequestMethod
                            | HttpLoggingFields.RequestQuery
                            | HttpLoggingFields.Duration;
});

// Add Reverse Proxy
var proxyBuilder = builder.Services.AddReverseProxy();
proxyBuilder.ConfigureHttpClient(static (_, handler) =>
{
    // this is required to decompress automatically
    handler.AutomaticDecompression = System.Net.DecompressionMethods.All; 
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseRouting();

try
{
    app.MapReverseProxy(static proxyBuilder =>
    {
        proxyBuilder.UseOutputCache();
        proxyBuilder.UseHttpLogging();
    });
    await app.RunAsync();
}
catch (OptionsValidationException ex)
{
    app.Logger.LogError("Options Validation Error: {OptionsName}: {Failures}", ex.OptionsName, ex.Failures);
    app.Logger.LogError("Exiting due to invalid options");
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "An unhandled exception has occurred");
    app.Logger.LogError("Exiting due to unhandled exception");
}