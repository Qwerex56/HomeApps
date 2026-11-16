using HomeMix.Application.Abstractions;
using HomeMix.Domain.Installations;
using HomeMix.Domain.Packages;
using HomeMix.Domain.Store;
using Microsoft.EntityFrameworkCore;

namespace HomeMix.Infrastructure.Persistence;

internal sealed class StoreStatsRepository(HomeMixDbContext db, TimeProvider time) : IStoreStatsRepository
{
    public void Add(StoreEvent? storeEvent)
    {
        if (storeEvent is not null)
        {
            db.StoreEvents.Add(storeEvent);
        }
    }

    public async Task<IReadOnlyDictionary<(string AddonId, string Source), StoreCounts>> CountsAsync(
        DateTimeOffset since30, CancellationToken cancellationToken)
    {
        var active = await ActiveInstallations()
            .GroupBy(x => new { x.AddonId, x.Repository, x.Tag, x.TargetKind })
            .Select(g => new { g.Key.AddonId, g.Key.Repository, g.Key.Tag, g.Key.TargetKind, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var events = await db.StoreEvents
            .GroupBy(e => new { e.AddonId, e.Source, e.Kind })
            .Select(g => new { g.Key.AddonId, g.Key.Source, g.Key.Kind, Count = g.Count(), Recent = g.Count(e => e.At >= since30) })
            .ToListAsync(cancellationToken);

        var counts = new Dictionary<(string, string), StoreCounts>();
        foreach (var a in active)
        {
            if (SourceOf(a.Repository, a.Tag) is not { } source)
            {
                continue;
            }

            var c = counts.GetValueOrDefault((a.AddonId, source), StoreCounts.None);
            counts[(a.AddonId, source)] = c with
            {
                Active = c.Active + a.Count,
                Households = c.Households + (a.TargetKind == TargetKind.Household ? a.Count : 0),
                Accounts = c.Accounts + (a.TargetKind == TargetKind.User ? a.Count : 0),
            };
        }

        foreach (var e in events)
        {
            var c = counts.GetValueOrDefault((e.AddonId, e.Source), StoreCounts.None);
            counts[(e.AddonId, e.Source)] = e.Kind switch
            {
                StoreEventKind.Installed => c with { Installs = e.Count, Installs30 = e.Recent },
                StoreEventKind.Updated => c with { Updates = e.Count },
                _ => c with { Removals = e.Count },
            };
        }

        return counts;
    }

    public async Task<IReadOnlyList<StoreVersionCount>> VersionsAsync(string addonId, string source, CancellationToken cancellationToken)
    {
        var rows = await ActiveInstallations()
            .Where(x => x.AddonId == addonId)
            .GroupBy(x => new { x.Version, x.Repository, x.Tag })
            .Select(g => new { g.Key.Version, g.Key.Repository, g.Key.Tag, Count = g.Count() })
            .ToListAsync(cancellationToken);
        return [.. rows.Where(r => SourceOf(r.Repository, r.Tag) == source)
            .GroupBy(r => r.Version)
            .Select(g => new StoreVersionCount(g.Key, g.Sum(r => r.Count)))
            .OrderByDescending(v => v.Installations).ThenBy(v => v.Version, StringComparer.Ordinal)];
    }

    public async Task<IReadOnlyList<StoreDay>> DailyInstallsAsync(string addonId, string source, DateTimeOffset since, CancellationToken cancellationToken)
    {
        var times = await db.StoreEvents
            .Where(e => e.AddonId == addonId && e.Source == source && e.Kind == StoreEventKind.Installed && e.At >= since)
            .Select(e => e.At)
            .ToListAsync(cancellationToken);
        var byDay = times.GroupBy(t => DateOnly.FromDateTime(t.UtcDateTime)).ToDictionary(g => g.Key, g => g.Count());
        var first = DateOnly.FromDateTime(since.UtcDateTime);
        var last = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime) is var today && today > first ? today : first;
        var days = new List<StoreDay>();
        for (var day = first; day <= last; day = day.AddDays(1))
        {
            days.Add(new StoreDay(day, byDay.GetValueOrDefault(day)));
        }

        return days;
    }

    public async Task<IReadOnlyDictionary<string, int>> HeartCountsAsync(CancellationToken cancellationToken) =>
        await db.StoreHearts.GroupBy(h => h.ListingKey)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Count, cancellationToken);

    public async Task<IReadOnlySet<string>> MyHeartsAsync(Guid userId, CancellationToken cancellationToken) =>
        (await db.StoreHearts.Where(h => h.UserId == userId).Select(h => h.ListingKey).ToListAsync(cancellationToken)).ToHashSet();

    public async Task<StoreHeart?> FindHeartAsync(string listingKey, Guid userId, CancellationToken cancellationToken) =>
        db.StoreHearts.Local.FirstOrDefault(h => h.ListingKey == listingKey && h.UserId == userId)
        ?? await db.StoreHearts.FirstOrDefaultAsync(h => h.ListingKey == listingKey && h.UserId == userId, cancellationToken);

    public void Add(StoreHeart heart) => db.StoreHearts.Add(heart);

    public void Remove(StoreHeart heart) => db.StoreHearts.Remove(heart);

    private IQueryable<ActiveRow> ActiveInstallations() =>
        from i in db.Installations
        where i.RemovedAt == null
        join p in db.Packages on i.PackageSha256 equals p.Sha256
        select new ActiveRow { AddonId = i.AddonId, Version = i.Version, TargetKind = i.TargetKind, Repository = p.Repository, Tag = p.Tag };

    private static string? SourceOf(string? repository, string? tag) =>
        repository ?? (tag == AddonPackage.OfficialTag ? ListingKey.OfficialSource : null);

    private sealed class ActiveRow
    {
        public required string AddonId { get; init; }

        public required string Version { get; init; }

        public required TargetKind TargetKind { get; init; }

        public string? Repository { get; init; }

        public string? Tag { get; init; }
    }
}
