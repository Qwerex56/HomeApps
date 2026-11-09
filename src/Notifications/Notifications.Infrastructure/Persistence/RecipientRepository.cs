using Microsoft.EntityFrameworkCore;
using Notifications.Application.Abstractions;
using Notifications.Domain.Recipients;

namespace Notifications.Infrastructure.Persistence;

internal sealed class RecipientRepository(NotificationsDbContext db) : IRecipientRepository
{
    public Task<Recipient?> FindAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Recipients.FirstOrDefaultAsync(r => r.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<Recipient>> GetManyAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
        await db.Recipients.Where(r => userIds.Contains(r.UserId)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Recipient>> FindByEndpointAsync(string endpoint, CancellationToken cancellationToken) =>
        await db.Recipients.Where(r => r.Devices.Any(d => d.Endpoint == endpoint)).ToListAsync(cancellationToken);

    public void Add(Recipient recipient) => db.Recipients.Add(recipient);

    public void Remove(Recipient recipient) => db.Recipients.Remove(recipient);
}
