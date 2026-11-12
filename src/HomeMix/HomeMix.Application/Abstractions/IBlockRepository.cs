using HomeMix.Domain.Blocks;

namespace HomeMix.Application.Abstractions;

/// <summary>The Owner's blocks; changes are saved by the unit of work after the command.</summary>
public interface IBlockRepository
{
    Task<IReadOnlyList<AddonBlock>> ListAsync(CancellationToken cancellationToken);

    Task<AddonBlock?> FindAsync(string value, CancellationToken cancellationToken);

    /// <summary>Whether the add-on id or the package's SHA-256 is blocked.</summary>
    Task<bool> AnyAsync(string addonId, string sha256, CancellationToken cancellationToken);

    void Add(AddonBlock block);

    void Remove(AddonBlock block);
}
