using HomeMix.Domain.Store;

namespace HomeMix.Application.Abstractions;

public interface IStoreListingRepository
{
    Task<StoreListing?> FindAsync(string key, CancellationToken cancellationToken);

    /// <summary>The official entry of a repository from addons.json, if it was read before.</summary>
    Task<StoreListing?> FindOfficialAsync(string repository, CancellationToken cancellationToken);

    /// <summary>Every entry with this status, newest first.</summary>
    Task<IReadOnlyList<StoreListing>> ListAsync(ListingStatus status, CancellationToken cancellationToken);

    /// <summary>Entries not removed and last read before <paramref name="before"/>, the oldest first.</summary>
    /// <summary>The entries a person submitted that are not removed, newest first.</summary>
    Task<IReadOnlyList<StoreListing>> ListByAuthorAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<StoreListing>> ListDueAsync(DateTimeOffset before, int take, CancellationToken cancellationToken);

    Task<int> CountSubmittedSinceAsync(Guid userId, DateTimeOffset since, CancellationToken cancellationToken);

    void Add(StoreListing listing);
}
