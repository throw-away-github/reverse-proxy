using Yarp.ReverseProxy.Configuration;

namespace CF.AccessProxy.Config.Validation;

public class DestinationHelper
{
    /// <summary>
    /// Converts a dictionary of urls to a dictionary of destination configs.
    /// </summary>
    /// <exception cref="ArgumentNullException">If the dictionary is null or empty.</exception>
    public static Dictionary<string, DestinationConfig> ConvertToDestinations(IDictionary<string, Uri> domains)
    {
        if (domains == null || !domains.Any())
            throw new ArgumentNullException(nameof(domains));
        
        return domains.ToDictionary(
            domain => domain.Key,
            domain => new DestinationConfig
            {
                Address = domain.Value.ToString()
            });
    }

}