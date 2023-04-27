using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace CF.AccessProxy.Config.Validation;

public partial class SemicolonSeparatedUrlsAttribute : ValidationAttribute
{
    // TODO: Add support for IPv4 and IPv6 addresses
    // TODO: See if options pattern can automatically convert string to dictionary/list of URLs
    private readonly Regex _urlRegex = UrlRegex();

    /// <summary>
    /// Data annotation to validate a semicolon-separated string of URLs.
    /// </summary>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string rawUrls)
        {
            return new ValidationResult("The value must be a string containing semicolon-separated URLs.");
        }

        var urls = rawUrls.Split(';').Select(url => url.Trim());

        foreach (var url in urls.Where(url => string.IsNullOrWhiteSpace(url) || !_urlRegex.IsMatch(url)))
        {
            return new ValidationResult($"Invalid URL: '{url}'", new[] { validationContext.MemberName }!);
        }

        return ValidationResult.Success;
    }

    [GeneratedRegex("^(https?|ftp|file)://.+$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex UrlRegex();
}