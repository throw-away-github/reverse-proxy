using System.Text.RegularExpressions;
using Yarp.ReverseProxy.Configuration;

namespace CF.AccessProxy.Config.Validation;

public partial class DestinationHelper
{
    /// <summary>
    /// Converts a semicolon separated string of urls to a dictionary of destinations.
    /// Keys are the subdomain, host, or full url depending on which is unique/available.
    /// </summary>
    /// <exception cref="ArgumentException">If the string is null or whitespace.</exception>
    public static Dictionary<string, DestinationConfig> ConvertStringToDestinations(string semicolonSeparatedUrls)
    {
        if (string.IsNullOrWhiteSpace(semicolonSeparatedUrls))
        {
            throw new ArgumentException("The argument cannot be null or whitespace.", nameof(semicolonSeparatedUrls));
        }

        var urls = semicolonSeparatedUrls.Split(';');
        var destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase);

        foreach (var url in urls)
        {
            var trimmedUrl = url.Trim();
            var subdomain = Subdomain(trimmedUrl);
            var config = new DestinationConfig { Address = trimmedUrl };

            if (destinations.TryAdd(subdomain, config))
                continue;
            if (destinations.TryAdd(new Uri(trimmedUrl).Host, config))
                continue;
            if (!destinations.TryAdd(trimmedUrl, config))
                throw new ArgumentException($"The URL '{trimmedUrl}' is a duplicate.", nameof(semicolonSeparatedUrls));
        }

        return destinations;
    }

    private static string Subdomain(string url)
    {
        var subdomain = SubdomainRegex().Match(url).Groups["subdomain"].Value;
        return string.IsNullOrWhiteSpace(subdomain) ? new Uri(url).Host : subdomain;
    }

    [GeneratedRegex(@"^https?://(?<subdomain>[^.]+)\..+$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex SubdomainRegex();
}