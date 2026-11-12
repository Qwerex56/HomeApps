using HomeMix.Domain.Installations;
using HomeMix.Domain.Manifests;

namespace HomeMix.Application.Installations.ListAllInstallations;

/// <summary>An installation as the Owner sees it: where, what was granted, from where, and whether it is blocked.</summary>
public sealed record AdminInstallationView(
    Guid Id,
    string AddonId,
    LocalizedText Name,
    string Version,
    string Sha256,
    TargetKind TargetKind,
    Guid TargetId,
    IReadOnlyList<string> Capabilities,
    IReadOnlyList<string> Network,
    string? Repository,
    Guid InstalledBy,
    DateTimeOffset InstalledAt,
    bool Blocked);
