using HomeMix.Application.Abstractions;

namespace HomeMix.Application.Store;

/// <summary>Adds the numbers and "domki" to store entries (homemix-2.md 2.2), read once for the whole list.</summary>
public sealed class StoreNumbers(IStoreStatsRepository stats, TimeProvider time)
{
    public async Task<IReadOnlyList<StoreEntryView>> AddAsync(IReadOnlyList<StoreEntryView> entries, Guid? me, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var counts = await stats.CountsAsync(time.GetUtcNow().AddDays(-30), cancellationToken);
        var hearts = await stats.HeartCountsAsync(cancellationToken);
        var mine = me is { } id ? await stats.MyHeartsAsync(id, cancellationToken) : new HashSet<string>();
        return [.. entries.Select(e =>
        {
            var c = counts.GetValueOrDefault((e.AddonId, e.Source), StoreCounts.None);
            return e with
            {
                Installs = c.Active,
                Installs30 = c.Installs30,
                Hearts = hearts.GetValueOrDefault(e.ListingKey),
                Hearted = mine.Contains(e.ListingKey),
            };
        })];
    }

    public async Task<StoreEntryView> AddAsync(StoreEntryView entry, Guid? me, CancellationToken cancellationToken) =>
        (await AddAsync([entry], me, cancellationToken))[0];
}
