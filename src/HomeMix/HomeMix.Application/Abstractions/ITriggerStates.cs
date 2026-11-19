using HomeMix.Domain.Triggers;

namespace HomeMix.Application.Abstractions;

/// <summary>How each trigger of each installation is doing; changes are saved by the unit of work.</summary>
public interface ITriggerStates
{
    Task<TriggerState?> FindAsync(Guid installationId, string triggerId, CancellationToken cancellationToken);

    Task<IReadOnlyList<TriggerState>> ListAsync(IReadOnlyCollection<Guid> installationIds, CancellationToken cancellationToken);

    void Add(TriggerState state);
}
