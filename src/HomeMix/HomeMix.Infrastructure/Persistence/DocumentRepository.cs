using HomeMix.Application.Abstractions;
using HomeMix.Domain.Data;
using Microsoft.EntityFrameworkCore;

namespace HomeMix.Infrastructure.Persistence;

internal sealed class DocumentRepository(HomeMixDbContext db) : IDocumentRepository
{
    public Task<AddonDocument?> FindAsync(DataSpace space, string collection, string key, CancellationToken cancellationToken) =>
        In(space, collection).FirstOrDefaultAsync(d => d.Key == key, cancellationToken);

    public async Task<IReadOnlyList<AddonDocument>> ListAsync(DataSpace space, string collection, CancellationToken cancellationToken) =>
        await In(space, collection).OrderBy(d => d.Key).ToListAsync(cancellationToken);

    public Task<int> CountAsync(DataSpace space, string collection, CancellationToken cancellationToken) =>
        In(space, collection).CountAsync(cancellationToken);

    public Task<long> UsedBytesAsync(Guid installationId, CancellationToken cancellationToken) =>
        db.Documents.Where(d => d.InstallationId == installationId).SumAsync(d => (long)d.Size, cancellationToken);

    public async Task<IReadOnlyList<AddonDocument>> ListOfAsync(IReadOnlyList<Guid> installationIds, CancellationToken cancellationToken) =>
        await db.Documents.Where(d => installationIds.Contains(d.InstallationId)).ToListAsync(cancellationToken);

    public Task<int> DeleteUnreachableAsync(DateTimeOffset removedBefore, CancellationToken cancellationToken) =>
        db.Documents
            .Where(d => !db.Installations.Any(i => i.Id == d.InstallationId && (i.RemovedAt == null || i.RemovedAt >= removedBefore)))
            .ExecuteDeleteAsync(cancellationToken);

    public void Add(AddonDocument document) => db.Documents.Add(document);

    public void Remove(AddonDocument document) => db.Documents.Remove(document);

    private IQueryable<AddonDocument> In(DataSpace space, string collection) =>
        db.Documents.Where(d =>
            d.InstallationId == space.InstallationId && d.Scope == space.Scope && d.OwnerUserId == space.OwnerUserId && d.Collection == collection);
}
