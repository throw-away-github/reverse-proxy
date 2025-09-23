namespace CF.AccessProxy.Config;

public record CacheOptions
{
    public TimeSpan StaleExpirationTimeSpan { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan DefaultExpirationTimeSpan { get; init; } = TimeSpan.FromMinutes(10);
}