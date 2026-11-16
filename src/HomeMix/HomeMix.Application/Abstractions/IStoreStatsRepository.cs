using HomeMix.Domain.Store;

namespace HomeMix.Application.Abstractions;

/// <summary>The store's numbers and "domki" (homemix-2.md 2.2). Sources are a repository or "official" (<see cref="StoreEvent.SourceOf"/>).</summary>
public interface IStoreStatsRepository
{
    /// <summary>Records an event; a null one (a zip) is not counted.</summary>
    void Add(StoreEvent? storeEvent);

    /// <summary>Every source's numbers, by add-on id and source.</summary>
    Task<IReadOnlyDictionary<(string AddonId, string Source), StoreCounts>> CountsAsync(DateTimeOffset since30, CancellationToken cancellationToken);

    Task<IReadOnlyList<StoreVersionCount>> VersionsAsync(string addonId, string source, CancellationToken cancellationToken);

    Task<IReadOnlyList<StoreDay>> DailyInstallsAsync(string addonId, string source, DateTimeOffset since, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, int>> HeartCountsAsync(CancellationToken cancellationToken);

    Task<IReadOnlySet<string>> MyHeartsAsync(Guid userId, CancellationToken cancellationToken);

    Task<StoreHeart?> FindHeartAsync(string listingKey, Guid userId, CancellationToken cancellationToken);

    void Add(StoreHeart heart);

    void Remove(StoreHeart heart);
}
