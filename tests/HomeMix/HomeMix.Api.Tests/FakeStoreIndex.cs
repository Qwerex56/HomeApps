using HomeMix.Application.Abstractions;

namespace HomeMix.Api.Tests;

/// <summary>addons.json's repositories, instead of reading the file.</summary>
public sealed class FakeStoreIndex : IStoreIndex
{
    public List<string> Repositories { get; } = [];

    public Task<IReadOnlyList<string>> RepositoriesAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<string>>([.. Repositories]);
}
