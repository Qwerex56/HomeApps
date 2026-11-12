using HomeMix.Domain.Installations;

namespace HomeMix.Application.Abstractions;

public interface IInstallationRepository
{
    Task<Installation?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>The installation of this add-on in this household or account, when it is installed there now.</summary>
    Task<Installation?> FindActiveAsync(string addonId, InstallTarget target, CancellationToken cancellationToken);

    Task<IReadOnlyList<Installation>> ListActiveAsync(InstallTarget target, CancellationToken cancellationToken);

    /// <summary>Ids of this add-on's installations in this household or account removed after <paramref name="since"/>.</summary>
    Task<IReadOnlyList<Guid>> RemovedSinceAsync(string addonId, InstallTarget target, DateTimeOffset since, CancellationToken cancellationToken);

    void Add(Installation installation);
}
