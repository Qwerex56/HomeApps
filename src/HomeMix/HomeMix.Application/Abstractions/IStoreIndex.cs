namespace HomeMix.Application.Abstractions;

/// <summary>
/// The store (W9): <c>addons.json</c> in a public repository lists GitHub repositories, and each is read from GitHub.
/// Nothing is uploaded to the store and nothing is kept but a short cache.
/// </summary>
public interface IStoreIndex
{
    Task<IReadOnlyList<StoreListing>> ListAsync(CancellationToken cancellationToken);
}
