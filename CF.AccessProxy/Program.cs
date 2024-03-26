using System.Net.Http.Headers;
using System.Text;
using CF.AccessProxy.Config;
using CF.AccessProxy.Config.Options;
using CF.AccessProxy.Extensions;
using CF.AccessProxy.Proxy.Clusters;
using CF.AccessProxy.Proxy.Routes;
using CF.AccessProxy.Proxy.Transforms;
using CF.AccessProxy.Services;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Load options, providers, and config
builder.Services
    .AddOptions<CFAccessOptions, CFAccessValidator>()
    .BindConfiguration(CFAccessOptions.Prefix)
    .ValidateOnStart();

builder.Services
    .AddOptions<GithubMetaOptions, GithubMetaValidator>()
    .BindConfiguration(GithubMetaOptions.Prefix)
    .ValidateOnStart();

builder.Services
    .RegisterAllTypes<IClusterProvider>()
    .RegisterAllTypes<IRouteProvider>()
    .AddSingleton<IProxyConfigInfo, InMemoryConfig>();

// Add Redis Cache
builder.Services
    .AddStackExchangeRedisCache(_ => { })
    .AddSingleton<IValidateOptions<RedisCacheOptions>, RedisCacheOptionsValidator>()
    .ConfigureOptions<RedisCacheOptionsConfigurator>()
    .AddOptions<RedisOptions>()
    .BindConfiguration(RedisOptions.Prefix);


builder.Services.AddHttpClient<GithubMetaService>()
    .ConfigureHttpClientUsing<IOptions<GithubMetaOptions>>((options, client) =>
    {
        client.BaseAddress = options.Value.GithubMetaApi;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.Token);
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        client.DefaultRequestHeaders.Add("User-Agent", "CF.AccessProxy");
        client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/vnd.github+json"));
    });

builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.RequestPath
                            | HttpLoggingFields.ResponseStatusCode
                            | HttpLoggingFields.RequestMethod
                            | HttpLoggingFields.Duration;
    options.CombineLogs = true;
});

builder.Services
    .AddTransient<ITransform, NugetIndexTransform>()
    .AddTransient<ITransform, CFAccessTransform>();

// Add Reverse Proxy
builder.Services.AddReverseProxy()
    .ConfigureHttpClient((_, handler) =>
    {
        // this is required to decompress automatically
        handler.AutomaticDecompression = System.Net.DecompressionMethods.All; 
    })
    .LoadFromProviders()
    .AddTransformFactory<SimpleTransformFactory>();

var app = builder.Build();

// Configure the HTTP request pipeline.
// app.UseHttpLogging();
app.UseRouting();

try
{
    app.MapReverseProxy();
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