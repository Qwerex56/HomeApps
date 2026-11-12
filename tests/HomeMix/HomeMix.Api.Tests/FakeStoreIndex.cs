using HomeMix.Application.Abstractions;

namespace HomeMix.Api.Tests;

/// <summary>The store's index as GitHub would describe it, instead of reading addons.json and GitHub.</summary>
public sealed class FakeStoreIndex : IStoreIndex
{
    public List<StoreListing> Listings { get; } = [];

    public Task<IReadOnlyList<StoreListing>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<StoreListing>>([.. Listings]);
}
