using Auth.Domain.ConnectedApps;

namespace Auth.Application.Abstractions;

/// <summary>Apps that registered themselves for OAuth (jarvis-v1.md 3.1).</summary>
public interface IAppClientRepository
{
    Task<AppClient?> GetByIdAsync(string id, CancellationToken cancellationToken);

    Task<IReadOnlyList<AppClient>> ListByIdsAsync(IReadOnlyCollection<string> ids, CancellationToken cancellationToken);

    Task<int> CountAsync(CancellationToken cancellationToken);

    void Add(AppClient client);
}
