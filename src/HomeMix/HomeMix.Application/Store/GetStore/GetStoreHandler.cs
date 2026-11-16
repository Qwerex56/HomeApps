using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using HomeMix.Application.Blocks;
using HomeMix.Domain.Installations;
using HomeMix.Domain.Manifests;
using HomeMix.Domain.Store;

namespace HomeMix.Application.Store.GetStore;

internal sealed class GetStoreHandler(
    IOfficialAddons builtin,
    IStoreListingRepository listings,
    IPackageRepository packages,
    Blocklist blocklist,
    StoreNumbers numbers,
    ICurrentUser currentUser)
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

        var counted = await numbers.AddAsync([.. Filter(entries, query)], currentUser.UserId, cancellationToken);
        return Order(counted, query.Tab).Skip((query.Page - 1) * StorePage.Size).Take(StorePage.Size).ToList();
    }

    private static IEnumerable<StoreEntryView> Filter(IEnumerable<StoreEntryView> entries, GetStoreQuery query)
    {
        if (query.Tab == StorePage.Featured)
        {
            entries = entries.Where(e => e.Official);
        }

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var q = query.Q.Trim();
            entries = entries.Where(e => Matches(e, q));
        }

        if (query.Target is { } target)
        {
            var kind = target == "household" ? TargetKind.Household : TargetKind.User;
            entries = entries.Where(e => e.Targets.Contains(kind));
        }

        if (query.Jarvis)
        {
            entries = entries.Where(e => e.Jarvis);
        }

        return query.Offline ? entries.Where(e => e.Network.Count == 0) : entries;
    }

    private static bool Matches(StoreEntryView entry, string q) =>
        entry.AddonId.Contains(q, StringComparison.OrdinalIgnoreCase)
        || Matches(entry.Name, q)
        || (entry.Description is { } description && Matches(description, q));

    private static bool Matches(LocalizedText text, string q) =>
        text.Pl.Contains(q, StringComparison.OrdinalIgnoreCase) || text.En.Contains(q, StringComparison.OrdinalIgnoreCase);

    // Stable sorts: ties keep the default order (official first, then newest).
    private static IEnumerable<StoreEntryView> Order(IEnumerable<StoreEntryView> entries, string? tab) => tab switch
    {
        StorePage.Popular => entries.OrderByDescending(e => e.Installs).ThenByDescending(e => e.Installs30),
        StorePage.Hearts => entries.OrderByDescending(e => e.Hearts),
        StorePage.New => entries.Where(e => e.SubmittedAt is not null).OrderByDescending(e => e.SubmittedAt),
        _ => entries,
    };
}
