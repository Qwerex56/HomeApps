using HomeMix.Application.Abstractions;
using HomeMix.Domain.Blocks;
using Microsoft.EntityFrameworkCore;

namespace HomeMix.Infrastructure.Persistence;

internal sealed class BlockRepository(HomeMixDbContext db) : IBlockRepository
{
    public async Task<IReadOnlyList<AddonBlock>> ListAsync(CancellationToken cancellationToken) =>
        await db.Blocks.OrderBy(b => b.BlockedAt).ToListAsync(cancellationToken);

    public async Task<AddonBlock?> FindAsync(string value, CancellationToken cancellationToken) =>
        db.Blocks.Local.FirstOrDefault(b => b.Value == value) ?? await db.Blocks.FirstOrDefaultAsync(b => b.Value == value, cancellationToken);

    public Task<bool> AnyAsync(string addonId, string sha256, CancellationToken cancellationToken) =>
        db.Blocks.AnyAsync(b => b.Value == addonId || b.Value == sha256, cancellationToken);

    public void Add(AddonBlock block) => db.Blocks.Add(block);

    public void Remove(AddonBlock block) => db.Blocks.Remove(block);
}
