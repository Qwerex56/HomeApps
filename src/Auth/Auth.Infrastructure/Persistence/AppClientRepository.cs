using Auth.Application.Abstractions;
using Auth.Domain.ConnectedApps;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Persistence;

internal sealed class AppClientRepository(AuthDbContext db) : IAppClientRepository
{
    public Task<AppClient?> GetByIdAsync(string id, CancellationToken cancellationToken) =>
        db.AppClients.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AppClient>> ListByIdsAsync(IReadOnlyCollection<string> ids, CancellationToken cancellationToken) =>
        await db.AppClients.Where(c => ids.Contains(c.Id)).ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken) => db.AppClients.CountAsync(cancellationToken);

    public void Add(AppClient client) => db.AppClients.Add(client);
}
