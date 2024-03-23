using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Options;
using Riok.Mapperly.Abstractions;
using StackExchange.Redis;
using BacklogPolicy = StackExchange.Redis.BacklogPolicy;

namespace CF.AccessProxy.Config.Options;

public class RedisOptions
{
    public const string Prefix = "Redis";

    /// <summary>
    /// Gets or sets the library name to use for CLIENT SETINFO lib-name calls to Redis during handshake.
    /// Defaults to "SE.Redis".
    /// </summary>
    /// <remarks>If the value is null, empty or whitespace, then the value from the options-provider will be used.</remarks>
    public string? LibraryName { get; set; }

    /// <summary>
    /// Gets or sets whether connect/configuration timeouts should be explicitly notified via a TimeoutException.
    /// </summary>
    public bool? AbortOnConnectFail { get; set; }

    /// <summary>
    /// Indicates whether admin operations should be allowed.
    /// </summary>
    public bool? AllowAdmin { get; set; }

    /// <summary>
    /// The backlog policy to be used for commands when a connection is unhealthy.
    /// </summary>
    public BacklogPolicy? BacklogPolicy { get; set; }

    /// <summary>
    /// A Boolean value that specifies whether the certificate revocation list is checked during authentication.
    /// </summary>
    public bool? CheckCertificateRevocation { get; set; }

    /// <summary>
    /// The number of times to repeat the initial connect cycle if no servers respond promptly.
    /// </summary>
    public int? ConnectRetry { get; set; }

    /// <summary>
    /// Specifies the time that should be allowed for connection.
    /// Falls back to Max(5000, SyncTimeout) if null.
    /// </summary>
    public int? ConnectTimeout { get; set; }

    /// <summary>
    /// Controls how often the connection heartbeats. A heartbeat includes:
    /// - Evaluating if any messages have timed out
    /// - Evaluating connection status (checking for failures)
    /// - Sending a server message to keep the connection alive if needed
    /// </summary>
    /// <remarks>Be aware setting this very low incurs additional overhead of evaluating the above more often.</remarks>
    public TimeSpan? HeartbeatInterval { get; set; }

    /// <summary>
    /// Should exceptions include identifiable details? (key names, additional .Data annotations)
    /// </summary>
    public bool? IncludeDetailInExceptions { get; set; }

    /// <summary>
    /// Should exceptions include performance counter details?
    /// </summary>
    /// <remarks>
    /// CPU usage, etc - note that this can be problematic on some platforms.
    /// </remarks>
    public bool? IncludePerformanceCountersInExceptions { get; set; }

    /// <summary>
    /// Specifies the time interval at which connections should be pinged to ensure validity.
    /// </summary>
    public TimeSpan? KeepAliveInterval { get; set; }

    /// <summary>
    /// Indicates whether endpoints should be resolved via DNS before connecting.
    /// If enabled the ConnectionMultiplexer will not re-resolve DNS when attempting to re-connect after a connection failure.
    /// </summary>
    public bool? ResolveDns { get; set; }

    /// <summary>
    /// Specifies the time that the system should allow for synchronous operations.
    /// </summary>
    public int? SyncTimeout { get; set; }

    /// <summary>
    /// Check configuration every n interval.
    /// </summary>
    public TimeSpan? ConfigCheckInterval { get; set; }

    /// <summary>
    /// The username to use to authenticate with the server.
    /// </summary>
    public string? User { get; set; }

    /// <summary>
    /// The password to use to authenticate with the server.
    /// </summary>
    public string? Password { get; set; }
}

[Mapper(AllowNullPropertyAssignment = false)]
[AutoConstructor]
public partial class RedisCacheOptionsConfigurator : IConfigureOptions<RedisCacheOptions>
{
    private readonly IConfiguration _configuration;
    private readonly IOptionsMonitor<RedisOptions> _redisOptions;

    public void Configure(RedisCacheOptions options)
    {
        options.Configuration = _configuration.GetConnectionString("Redis");
        if (options is { Configuration: not null, ConfigurationOptions: null })
            options.ConfigurationOptions = ConfigurationOptions.Parse(options.Configuration);
        
        options.ConfigurationOptions ??= new ConfigurationOptions();

        var otherOptions = _redisOptions.CurrentValue;
        Map(otherOptions, options.ConfigurationOptions);
    }
    
    private partial void Map(RedisOptions redisOptions, ConfigurationOptions config);
}

internal sealed class RedisCacheOptionsValidator : IValidateOptions<RedisCacheOptions>
{
    public ValidateOptionsResult Validate(string? name, RedisCacheOptions options)
    {
        if (options is not { ConfigurationOptions.EndPoints.Count: > 0 })
            return ValidateOptionsResult.Fail("No Redis endpoints are configured.");

        return ValidateOptionsResult.Success;
    }
}