using HomeMix.Domain.Installations;

namespace HomeMix.Domain.Manifests;

/// <summary>
/// An add-on's <c>homemix.json</c> after <see cref="AddonManifestParser"/> checked it (docs/homemix.md). The original
/// JSON is kept next to it in the package, so fields added by later stages (tools) are never lost.
/// </summary>
public sealed record AddonManifest(
    string Id,
    string Version,
    string RequiresHomeApps,
    LocalizedText Name,
    LocalizedText? Description,
    string Icon,
    IReadOnlyList<TargetKind> Targets,
    string Entry,
    IReadOnlyList<AddonPage> Pages,
    IReadOnlyList<AddonWidget> Widgets,
    IReadOnlyList<string> Capabilities,
    IReadOnlyList<string> Network,
    IReadOnlyList<AddonPermission> Permissions,
    bool Jarvis);
