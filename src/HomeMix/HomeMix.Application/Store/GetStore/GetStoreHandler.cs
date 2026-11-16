using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using HomeMix.Application.Blocks;
using HomeMix.Domain.Store;

namespace HomeMix.Application.Store.GetStore;

internal sealed class GetStoreHandler(
    IOfficialAddons builtin, IStoreListingRepository listings, IPackageRepository packages, Blocklist blocklist, ICurrentUser currentUser)
    : IQueryHandler<GetStoreQuery, IReadOnlyList<StoreEntryView>>
{
    public async Task<Result<IReadOnlyList<StoreEntryView>>> Handle(GetStoreQuery query, CancellationToken cancellationToken)
    {
        var blocked = await blocklist.SnapshotAsync(cancellationToken);
        var images = builtin.List().Where(o => !blocked(o.Manifest.Id, o.Sha256)).ToList();
        var entries = images.Select(o => StoreEntryView.FromBuiltin(o.Manifest, o.Sha256)).ToList();

        var listed = (await listings.ListAsync(ListingStatus.Listed, cancellationToken))
            .Where(l => !blocked(l.AddonId, l.PackageSha256))
            .Where(l => !l.Official || images.All(o => o.Manifest.Id != l.AddonId))
            .OrderByDescending(l => l.Official)
            .ToList();
        var manifests = (await packages.FindManyAsync(listed.Select(l => l.PackageSha256).ToList(), cancellationToken))
            .ToDictionary(p => p.Sha256, p => p.Manifest, StringComparer.Ordinal);
        entries.AddRange(listed
            .Where(l => manifests.ContainsKey(l.PackageSha256))
            .Select(l => StoreEntryView.From(l, manifests[l.PackageSha256], currentUser.UserId)));

        return entries;
    }
}
