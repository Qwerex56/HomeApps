using HomeMix.Application.Abstractions;
using HomeMix.Domain.Installations;
using Microsoft.EntityFrameworkCore;

namespace HomeMix.Infrastructure.Persistence;

internal sealed class InstallationRepository(HomeMixDbContext db) : IInstallationRepository
{
    public Task<Installation?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.Installations.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public Task<Installation?> FindActiveAsync(string addonId, InstallTarget target, CancellationToken cancellationToken) =>
        db.Installations.FirstOrDefaultAsync(
            i => i.AddonId == addonId && i.TargetKind == target.Kind && i.TargetId == target.Id && i.RemovedAt == null, cancellationToken);

    public async Task<IReadOnlyList<Installation>> ListActiveAsync(InstallTarget target, CancellationToken cancellationToken) =>
        await db.Installations
            .Where(i => i.TargetKind == target.Kind && i.TargetId == target.Id && i.RemovedAt == null)
            .ToListAsync(cancellationToken);

    public void Add(Installation installation) => db.Installations.Add(installation);
}
