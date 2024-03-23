namespace CF.AccessProxy.Models;

public record GithubMeta
{
    public required bool VerifiablePasswordAuthentication { get; init; }
    public required SshKeyFingerprints SshKeyFingerprints { get; init; }
    public required IReadOnlyList<string> SshKeys { get; init; }
    public required IReadOnlyList<string> Hooks { get; init; }
    public required IReadOnlyList<string> Web { get; init; }
    public required IReadOnlyList<string> Api { get; init; }
    public required IReadOnlyList<string> Git { get; init; }
    public required IReadOnlyList<string> GithubEnterpriseImporter { get; init; }
    public required IReadOnlyList<string> Packages { get; init; }
    public required IReadOnlyList<string> Pages { get; init; }
    public required IReadOnlyList<string> Importer { get; init; }
    public required IReadOnlyList<IpNetwork> Actions { get; init; }
    public required IReadOnlyList<string> Dependabot { get; init; }
    public required Domains Domains { get; init; }
}

public record Domains
{
    public required IReadOnlyList<string> Website { get; init; }
    public required IReadOnlyList<string> Copilot { get; init; }
    public required IReadOnlyList<string> Packages { get; init; }
    public required IReadOnlyList<string> Actions { get; init; }
}

public record SshKeyFingerprints
{
    public required string Sha256Ecdsa { get; init; }
    public required string Sha256Ed25519 { get; init; }
    public required string Sha256Rsa { get; init; }
}