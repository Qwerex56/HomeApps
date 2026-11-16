using HomeMix.Application.Abstractions;
using HomeMix.Domain.Store;
using Microsoft.EntityFrameworkCore;

namespace HomeMix.Infrastructure.Persistence;

internal sealed class StoreListingRepository(HomeMixDbContext db) : IStoreListingRepository
{
    public async Task<StoreListing?> FindAsync(string key, CancellationToken cancellationToken) =>
        db.StoreListings.Local.FirstOrDefault(l => l.Key == key)
        ?? await db.StoreListings.FirstOrDefaultAsync(l => l.Key == key, cancellationToken);

    public async Task<StoreListing?> FindOfficialAsync(string repository, CancellationToken cancellationToken) =>
        db.StoreListings.Local.FirstOrDefault(l => l.Official && l.Repository == repository)
        ?? await db.StoreListings.FirstOrDefaultAsync(l => l.Official && l.Repository == repository, cancellationToken);

    public async Task<IReadOnlyList<StoreListing>> ListAsync(ListingStatus status, CancellationToken cancellationToken) =>
        await db.StoreListings.Where(l => l.Status == status).OrderByDescending(l => l.SubmittedAt).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<StoreListing>> ListByAuthorAsync(Guid userId, CancellationToken cancellationToken) =>
        await db.StoreListings.Where(l => l.SubmittedBy == userId && l.Status != ListingStatus.Removed)
            .OrderByDescending(l => l.SubmittedAt).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<StoreListing>> ListDueAsync(DateTimeOffset before, int take, CancellationToken cancellationToken) =>
        await db.StoreListings.Where(l => l.Status != ListingStatus.Removed && l.RefreshedAt < before)
            .OrderBy(l => l.RefreshedAt).Take(take).ToListAsync(cancellationToken);

    public Task<int> CountSubmittedSinceAsync(Guid userId, DateTimeOffset since, CancellationToken cancellationToken) =>
        db.StoreListings.CountAsync(l => l.SubmittedBy == userId && l.LastSubmittedAt >= since, cancellationToken);

    public void Add(StoreListing listing) => db.StoreListings.Add(listing);
}
