using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace CF.AccessProxy.Config.Validation;

public partial class SemicolonSeparatedUrlsAttribute : ValidationAttribute
{
    private readonly Regex _urlRegex = UrlRegex();

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string rawUrls)
        {
            return new ValidationResult("The value must be a string containing semicolon-separated URLs.");
        }

        var urls = rawUrls.Split(';').Select(url => url.Trim()).ToList();

        foreach (var url in urls.Where(url => string.IsNullOrWhiteSpace(url) || !_urlRegex.IsMatch(url)))
        {
            return new ValidationResult($"Invalid URL: '{url}'", new[] { validationContext.MemberName }!);
        }

        return ValidationResult.Success;
    }

    [GeneratedRegex("^(https?|ftp|file)://.+$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex UrlRegex();
}