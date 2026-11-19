using HomeMix.Application.Abstractions;
using HomeMix.Domain.Triggers;
using Microsoft.EntityFrameworkCore;

namespace HomeMix.Infrastructure.Persistence;

internal sealed class TriggerStateRepository(HomeMixDbContext db) : ITriggerStates
{
    public Task<TriggerState?> FindAsync(Guid installationId, string triggerId, CancellationToken cancellationToken) =>
        db.TriggerStates.FirstOrDefaultAsync(s => s.InstallationId == installationId && s.TriggerId == triggerId, cancellationToken);

    public async Task<IReadOnlyList<TriggerState>> ListAsync(IReadOnlyCollection<Guid> installationIds, CancellationToken cancellationToken) =>
        await db.TriggerStates.Where(s => installationIds.Contains(s.InstallationId)).AsNoTracking().ToListAsync(cancellationToken);

    public void Add(TriggerState state) => db.TriggerStates.Add(state);
}
