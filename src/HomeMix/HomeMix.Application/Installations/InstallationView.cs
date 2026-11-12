using HomeMix.Domain.Installations;
using HomeMix.Domain.Manifests;
using HomeMix.Domain.Packages;

namespace HomeMix.Application.Installations;

/// <summary>An installed add-on as the web app needs it to show its pages and tiles, and what was granted to it.</summary>
public sealed record InstallationView(
    Guid Id,
    string AddonId,
    string Version,
    TargetKind TargetKind,
    Guid TargetId,
    LocalizedText Name,
    string Icon,
    string RunUrl,
    string Entry,
    IReadOnlyList<AddonPage> Pages,
    IReadOnlyList<AddonWidget> Widgets,
    IReadOnlyList<AddonPermission> Permissions,
    IReadOnlyList<string> Capabilities,
    IReadOnlyList<string> Network,
    bool JarvisAllowed,
    string? Repository,
    Guid InstalledBy,
    DateTimeOffset InstalledAt,
    DateTimeOffset? UpdatedAt)
{
    public static InstallationView From(Installation installation, AddonPackage package)
    {
        ArgumentNullException.ThrowIfNull(installation);
        ArgumentNullException.ThrowIfNull(package);
        var m = package.Manifest;
        return new InstallationView(
            installation.Id, installation.AddonId, installation.Version, installation.TargetKind, installation.TargetId,
            m.Name, m.Icon, $"/homemix/run/{installation.Id}/", m.Entry, m.Pages, m.Widgets, m.Permissions,
            installation.Capabilities, installation.Network, installation.JarvisAllowed, package.Repository,
            installation.InstalledBy, installation.InstalledAt, installation.UpdatedAt);
    }
}
