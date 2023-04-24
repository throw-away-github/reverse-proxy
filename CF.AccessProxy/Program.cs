using CF.AccessProxy;
using CF.AccessProxy.Clusters;
using CF.AccessProxy.Config;
using CF.AccessProxy.Routes;
using CF.AccessProxy.Transforms;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Load options, providers, and config
builder.Services
    .LoadProviderOptions()
    .RegisterAllTypes<IClusterProvider>()
    .RegisterAllTypes<IRouteProvider>()
    .AddSingleton<IProxyConfigInfo, InMemoryConfig>();

// Add Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromProviders()
    .AddTransforms<LeadingPathToHost>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpLogging();
app.UseRouting();
app.MapReverseProxy();

app.Run();