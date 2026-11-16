using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;

namespace HomeMix.Application.Store.GetMyListings;

internal sealed class GetMyListingsHandler(
    IStoreListingRepository listings,
    IPackageRepository packages,
    IStoreStatsRepository stats,
    StoreNumbers numbers,
    ICurrentUser currentUser,
    TimeProvider time)
    : IQueryHandler<GetMyListingsQuery, IReadOnlyList<MyListingView>>
{
    public async Task<Result<IReadOnlyList<MyListingView>>> Handle(GetMyListingsQuery query, CancellationToken cancellationToken)
    {
        var me = currentUser.RequiredUserId;
        var mine = await listings.ListByAuthorAsync(me, cancellationToken);
        var manifests = (await packages.FindManyAsync(mine.Select(l => l.PackageSha256).ToList(), cancellationToken))
            .ToDictionary(p => p.Sha256, p => p.Manifest, StringComparer.Ordinal);
        var entries = await numbers.AddAsync(
            [.. mine.Where(l => manifests.ContainsKey(l.PackageSha256)).Select(l => StoreEntryView.From(l, manifests[l.PackageSha256], me))],
            me,
            cancellationToken);

        var now = time.GetUtcNow();
        var counts = await stats.CountsAsync(now.AddDays(-30), cancellationToken);
        var views = new List<MyListingView>(entries.Count);
        foreach (var entry in entries)
        {
            views.Add(new MyListingView(
                entry,
                counts.GetValueOrDefault((entry.AddonId, entry.Source), StoreCounts.None),
                await stats.VersionsAsync(entry.AddonId, entry.Source, cancellationToken),
                await stats.DailyInstallsAsync(entry.AddonId, entry.Source, now.AddDays(-29), cancellationToken)));
        }

        return views;
    }
}
