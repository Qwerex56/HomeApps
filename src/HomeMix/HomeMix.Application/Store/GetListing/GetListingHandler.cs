using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using HomeMix.Application.Blocks;
using HomeMix.Domain.Store;

namespace HomeMix.Application.Store.GetListing;

internal sealed class GetListingHandler(
    IStoreListingRepository listings, IPackageRepository packages, IOfficialAddons builtin, Blocklist blocklist, StoreNumbers numbers, ICurrentUser currentUser)
    : IQueryHandler<GetListingQuery, StoreEntryView>
{
    public async Task<Result<StoreEntryView>> Handle(GetListingQuery query, CancellationToken cancellationToken)
    {
        var entry = await FindAsync(query, cancellationToken);
        return entry.IsSuccess ? await numbers.AddAsync(entry.Value, currentUser.UserId, cancellationToken) : entry;
    }

    private async Task<Result<StoreEntryView>> FindAsync(GetListingQuery query, CancellationToken cancellationToken)
    {
        var blocked = await blocklist.SnapshotAsync(cancellationToken);
        var image = builtin.List().FirstOrDefault(o => ListingKey.Official(o.Manifest.Id) == query.Key);
        if (image is not null)
        {
            return blocked(image.Manifest.Id, image.Sha256)
                ? Result.Fail(StoreErrors.NotFound)
                : StoreEntryView.FromBuiltin(image.Manifest, image.Sha256);
        }

        var me = currentUser.UserId;
        var listing = await listings.FindAsync(query.Key, cancellationToken);
        var visible = listing is not null && (listing.Status == ListingStatus.Listed
            || (listing.Status == ListingStatus.Hidden && ((me is { } id && listing.IsAuthor(id)) || currentUser.IsInRole("Owner"))));
        if (!visible || blocked(listing!.AddonId, listing.PackageSha256))
        {
            return Result.Fail(StoreErrors.NotFound);
        }

        return await StoreEntries.ViewAsync(listing, packages, me, cancellationToken);
    }
}
