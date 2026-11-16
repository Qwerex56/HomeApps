using HomeMix.Domain.Manifests;
using HomeMix.Domain.Installations;
using HomeMix.Domain.Store;

namespace HomeMix.Application.Store;

/// <summary>
/// One add-on in the store. <see cref="Builtin"/> ones came with this HomeMix and are added by
/// <see cref="AddonId"/>; the rest are added from <see cref="Repository"/> like any GitHub link. <see cref="Official"/>
/// marks the team's (built in or from addons.json); <see cref="ListingKey"/> names the entry (homemix-2.md S3).
/// </summary>
public sealed record StoreEntryView(
    string ListingKey,
    bool Official,
    bool Builtin,
    string AddonId,
    string? Repository,
    LocalizedText Name,
    LocalizedText? Description,
    string Icon,
    string Version,
    string Sha256,
    string? Readme,
    DateTimeOffset? PublishedAt,
    string? Problem,
    IReadOnlyList<string> Capabilities,
    IReadOnlyList<string> Network,
    bool Jarvis,
    IReadOnlyList<TargetKind> Targets,
    string Status,
    bool Mine)
{
    public static StoreEntryView From(StoreListing listing, AddonManifest manifest, Guid? me) => new(
        listing.Key, listing.Official, false, listing.AddonId, listing.Repository, manifest.Name, manifest.Description, manifest.Icon,
        manifest.Version, listing.PackageSha256, listing.Readme, listing.PublishedAt, listing.Problem, manifest.Capabilities,
        manifest.Network, manifest.Jarvis, manifest.Targets, listing.Status.ToString(), me is { } id && listing.IsAuthor(id));

    public static StoreEntryView FromBuiltin(AddonManifest manifest, string sha256) => new(
        Domain.Store.ListingKey.Official(manifest.Id), true, true, manifest.Id, null, manifest.Name, manifest.Description, manifest.Icon,
        manifest.Version, sha256, null, null, null, manifest.Capabilities, manifest.Network, manifest.Jarvis, manifest.Targets,
        nameof(ListingStatus.Listed), false);
}
