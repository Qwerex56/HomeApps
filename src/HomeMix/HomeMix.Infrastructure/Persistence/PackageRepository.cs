using HomeMix.Application.Abstractions;
using HomeMix.Domain.Packages;
using Microsoft.EntityFrameworkCore;

namespace HomeMix.Infrastructure.Persistence;

internal sealed class PackageRepository(HomeMixDbContext db) : IPackageRepository
{
    public async Task<AddonPackage?> FindAsync(string sha256, CancellationToken cancellationToken) =>
        db.Packages.Local.FirstOrDefault(p => p.Sha256 == sha256)
        ?? await db.Packages.FirstOrDefaultAsync(p => p.Sha256 == sha256, cancellationToken);

    public async Task<IReadOnlyList<AddonPackage>> FindManyAsync(IReadOnlyCollection<string> sha256s, CancellationToken cancellationToken) =>
        await db.Packages.Where(p => sha256s.Contains(p.Sha256)).ToListAsync(cancellationToken);

    public void Add(AddonPackage package) => db.Packages.Add(package);
}
