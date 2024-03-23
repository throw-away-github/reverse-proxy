using System.Net;
using System.Text.Json;
using CF.AccessProxy.Models;
using Microsoft.Extensions.Caching.Distributed;

namespace CF.AccessProxy.Services;

[AutoConstructor]
public partial class GithubMetaService
{
    private readonly HttpClient _client;
    private readonly IDistributedCache _cache;
    private readonly ILogger<GithubMetaService> _logger;
    
    public async Task<bool> IsGithubIpAsync(IPAddress ip)
    {
        var key = $"github-ip:{ip}";
        var cached = await _cache.GetStringAsync(key);
        if (bool.TryParse(cached, out var result))
            return result;
        
        var ipNetworks = await GetGithubIpsAsync();
        if (!ipNetworks.Any())
        {
            // likely a network error, block the request but don't cache it
            return false;
        }
        
        if (ipNetworks.Any(ipn => ipn.Contains(ip)))
        {
            await _cache.SetStringAsync(key, bool.TrueString);
            return true;
        }
        await _cache.SetStringAsync(key, bool.FalseString);
        return false;
    }
    
    private async Task<IReadOnlyList<IpNetwork>> GetGithubIpsAsync()
    {
        var cached = await _cache.GetStringAsync("github-ips");
        if (!string.IsNullOrWhiteSpace(cached))
            return JsonSerializer.Deserialize(cached, JsonContext.Default.IPNetworkSet) ?? [];

        var response = await _client.GetAsync("https://api.github.com/meta");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to fetch Github meta data: {StatusCode}", response.StatusCode);
            return [];
        }

        var meta = await response.Content.ReadFromJsonAsync(JsonContext.Default.GithubMeta);
        if (meta == null)
        {
            _logger.LogError("Failed to parse Github meta data");
            return [];
        }
        
        var ipNetworks = meta.Actions;
        var json = JsonSerializer.Serialize(ipNetworks, JsonContext.Default.IPNetworkSet);
        await _cache.SetStringAsync("github-ips", json);
        return ipNetworks;
    }
    
}