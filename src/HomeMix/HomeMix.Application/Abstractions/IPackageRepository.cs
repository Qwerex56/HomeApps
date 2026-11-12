using HomeMix.Domain.Packages;

namespace HomeMix.Application.Abstractions;

/// <summary>Packages by their SHA-256; changes are saved by the unit of work after the command.</summary>
public interface IPackageRepository
{
    Task<AddonPackage?> FindAsync(string sha256, CancellationToken cancellationToken);

    Task<IReadOnlyList<AddonPackage>> FindManyAsync(IReadOnlyCollection<string> sha256s, CancellationToken cancellationToken);

    void Add(AddonPackage package);
}
