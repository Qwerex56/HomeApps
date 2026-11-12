using HomeMix.Domain.Installations;
using HomeMix.Domain.Manifests;
using HomeMix.Domain.Packages;

namespace HomeMix.Application.Packages;

/// <summary>A package as the consent screen shows it, before anything is installed.</summary>
public sealed record PackageView(
    string Sha256,
    string Id,
    string Version,
    LocalizedText Name,
    LocalizedText? Description,
    string Icon,
    IReadOnlyList<TargetKind> Targets,
    IReadOnlyList<string> Capabilities,
    IReadOnlyList<string> Network,
    bool Jarvis,
    string RequiresHomeApps,
    string? Repository,
    string? Tag,
    bool Official,
    long Size)
{
    public static PackageView From(AddonPackage package)
    {
        ArgumentNullException.ThrowIfNull(package);
        var m = package.Manifest;
        return new PackageView(
            package.Sha256, m.Id, m.Version, m.Name, m.Description, m.Icon, m.Targets, m.Capabilities, m.Network, m.Jarvis,
            m.RequiresHomeApps, package.Repository, package.Tag, package.IsOfficial, package.Size);
    }
}
