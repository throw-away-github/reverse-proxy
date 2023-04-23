using CF.AccessProxy;
using CF.AccessProxy.Clusters;
using CF.AccessProxy.Config;
using CF.AccessProxy.Config.Options;
using CF.AccessProxy.Routes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Load config
builder.Services.LoadOptions<CFAccessOptions>();
// Add Cluster and Route Providers
builder.Services
    .AddTransient<IClusterProvider, SeedBoxCluster>()
    .AddTransient<IRouteProvider, CFAccessRoute>()
    .AddSingleton<IProxyConfigInfo, InMemoryConfig>();

// Add Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromProviders();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseRouting();
app.MapReverseProxy();

app.Run();