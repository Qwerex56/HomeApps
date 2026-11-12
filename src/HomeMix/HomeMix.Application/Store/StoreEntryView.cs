using HomeMix.Domain.Manifests;

namespace HomeMix.Application.Store;

/// <summary>
/// One add-on in the store. Official ones come with HomeMix and are added by <see cref="AddonId"/>; the rest are added
/// from <see cref="Repository"/> like any GitHub link, and the installer picks a household or the account.
/// </summary>
public sealed record StoreEntryView(
    bool Official,
    string? AddonId,
    string? Repository,
    LocalizedText Name,
    LocalizedText? Description,
    string Icon,
    string? Version,
    string? Sha256,
    string? Readme,
    DateTimeOffset? PublishedAt,
    string? Problem);
