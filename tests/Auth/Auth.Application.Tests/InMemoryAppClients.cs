using Auth.Application.Abstractions;
using Auth.Domain.ConnectedApps;

namespace Auth.Application.Tests;

internal sealed class InMemoryAppClients : IAppClientRepository
{
    public List<AppClient> Items { get; } = [];

    public Task<AppClient?> GetByIdAsync(string id, CancellationToken cancellationToken) =>
        Task.FromResult(Items.Find(c => c.Id == id));

    public Task<IReadOnlyList<AppClient>> ListByIdsAsync(IReadOnlyCollection<string> ids, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AppClient>>([.. Items.Where(c => ids.Contains(c.Id))]);

    public Task<int> CountAsync(CancellationToken cancellationToken) => Task.FromResult(Items.Count);

    public void Add(AppClient client) => Items.Add(client);
}
