using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace CF.AccessProxy.Config.Options;

internal enum TokenType
{
    [Display(Name = "Bearer")] Bearer,
    [Display(Name = "Basic")] Basic
}

internal sealed record GithubMetaOptions
{
    public static string Prefix => "Github";
    [Required] public Uri GithubMetaApi { get; init; } = new("https://api.github.com/meta");
    [Required] public string Token { get; init; }
}

[OptionsValidator]
internal sealed partial class GithubMetaValidator : IValidateOptions<GithubMetaOptions>;