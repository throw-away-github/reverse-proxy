using Yarp.ReverseProxy.Configuration;

namespace CF.AccessProxy.Config.Validation;

public static class DestinationHelper
{
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
            destinations.Add(CreateDestinationName(trimmedUrl), new DestinationConfig { Address = trimmedUrl });
        }

        return destinations;
    }

    private static string CreateDestinationName(string url)
    {
        var name = url.Replace("https://", "").Replace("http://", "");
        var slashIndex = name.IndexOf('/');
        if (slashIndex >= 0)
        {
            name = name[..slashIndex];
        }
        return name;
    }
}