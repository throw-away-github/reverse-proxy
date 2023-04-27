using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Yarp.ReverseProxy.Configuration;

namespace CF.AccessProxy.Config.Validation;

public partial class DestinationHelper
{
    /// <summary>
    /// Converts a semicolon separated string of urls to a dictionary of destinations.
    /// Hostname is used as the key.
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
            destinations.Add(subdomain, new DestinationConfig { Address = trimmedUrl });
        }

        return destinations;
    }

    private static string Subdomain(string url)
    {
        var subdomain = SubdomainRegex().Match(url).Groups["subdomain"].Value;
        return string.IsNullOrWhiteSpace(subdomain) ? url : subdomain;
    }
    
    [GeneratedRegex(@"^https?://(?<subdomain>[^.]+)\..+$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex SubdomainRegex();
}