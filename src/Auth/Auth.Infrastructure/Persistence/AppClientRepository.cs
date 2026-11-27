using Auth.Application.Abstractions;
using Auth.Domain.ConnectedApps;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Persistence;

/// <summary>
/// Registered apps from the database, plus the first-party ones from <c>Auth:Clients</c>: a native client with redirect
/// URIs (HomeApps.Desktop, <c>homeapps-desktop</c>) is an OAuth client without registration.
/// </summary>
internal sealed class AppClientRepository(AuthDbContext db, IClientRegistry registry) : IAppClientRepository
{
    public async Task<AppClient?> GetByIdAsync(string id, CancellationToken cancellationToken) =>
        FirstParty(id) ?? await db.AppClients.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AppClient>> ListByIdsAsync(IReadOnlyCollection<string> ids, CancellationToken cancellationToken)
    {
        var stored = await db.AppClients.Where(c => ids.Contains(c.Id)).ToListAsync(cancellationToken);
        return [.. stored, .. ids.Select(FirstParty).OfType<AppClient>()];
    }

    public Task<int> CountAsync(CancellationToken cancellationToken) => db.AppClients.CountAsync(cancellationToken);

    public void Add(AppClient client) => db.AppClients.Add(client);

    private AppClient? FirstParty(string id) =>
        registry.Find(id) is { Type: ClientType.Native, RedirectUris.Count: > 0 } client
            ? AppClient.FirstParty(client.ClientId, client.Name, client.RedirectUris.Select(u => u.OriginalString))
            : null;
}
