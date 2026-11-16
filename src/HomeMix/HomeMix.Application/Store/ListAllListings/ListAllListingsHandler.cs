using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Store;

namespace HomeMix.Application.Store.ListAllListings;

internal sealed class ListAllListingsHandler(
    IStoreListingRepository listings, IPackageRepository packages, StoreNumbers numbers, ICurrentUser currentUser)
    : IQueryHandler<ListAllListingsQuery, IReadOnlyList<StoreEntryView>>
{
    public async Task<Result<IReadOnlyList<StoreEntryView>>> Handle(ListAllListingsQuery query, CancellationToken cancellationToken)
    {
        var all = (await listings.ListAsync(ListingStatus.Listed, cancellationToken))
            .Concat(await listings.ListAsync(ListingStatus.Hidden, cancellationToken))
            .OrderByDescending(l => l.SubmittedAt)
            .ToList();
        var manifests = (await packages.FindManyAsync(all.Select(l => l.PackageSha256).ToList(), cancellationToken))
            .ToDictionary(p => p.Sha256, p => p.Manifest, StringComparer.Ordinal);
        var entries = all.Where(l => manifests.ContainsKey(l.PackageSha256))
            .Select(l => StoreEntryView.From(l, manifests[l.PackageSha256], currentUser.UserId))
            .ToList();
        return Result.Ok(await numbers.AddAsync(entries, currentUser.UserId, cancellationToken));
    }
}
