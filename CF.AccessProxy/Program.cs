using System.Text;
using CF.AccessProxy.Config;
using CF.AccessProxy.Extensions;
using CF.AccessProxy.Proxy.Clusters;
using CF.AccessProxy.Proxy.Routes;
using CF.AccessProxy.Services;
using Microsoft.AspNetCore.HttpLogging;
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
    .AddSingleton<IProxyConfigInfo, InMemoryConfig>();


builder.Services.AddHttpClient<StaleWhileRevalidateCachePolicy>();

builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.RequestPath
                            | HttpLoggingFields.ResponseStatusCode
                            | HttpLoggingFields.RequestMethod
                            | HttpLoggingFields.Duration;
    options.CombineLogs = true;
});

builder.Services.AddSingleton<WorkerProcessor<string>>();

builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(policy =>
    {
        policy.SetVaryByHost(true);
        policy.AddPolicy<StaleWhileRevalidateCachePolicy>();
    });
    options.DefaultExpirationTimeSpan = TimeSpan.FromMinutes(5);
});

// Add Reverse Proxy
builder.Services.AddReverseProxy()
    .ConfigureHttpClient((_, handler) =>
    {
        // this is required to decompress automatically
        handler.AutomaticDecompression = System.Net.DecompressionMethods.All; 
    })
    .LoadFromProviders();

var app = builder.Build();

// Configure the HTTP request pipeline.
// app.UseHttpLogging();
app.UseRouting();

try
{
    app.MapReverseProxy(configureApp =>
    {
        configureApp.UseOutputCache();
    });
    await app.RunAsync();
}
catch (OptionsValidationException ex)
{
    var sb = new StringBuilder();
    sb.AppendLine($"Failed to validate options {ex.OptionsName}:");
    foreach (var failure in ex.Failures)
    {
        sb.AppendLine(failure);
    }
    app.Logger.LogError("Options Validation Error: {ValidationErrors}", sb.ToString());
    app.Logger.LogError("Exiting due to invalid options");
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "An unhandled exception has occurred");
    app.Logger.LogError("Exiting due to unhandled exception");
}