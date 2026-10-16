namespace HomeApps.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Commits everything the current request changed, dispatches domain events and writes outbox messages in one transaction.
/// Implemented by the service's DbContext. Handlers normally don't call it: the transaction behavior does after a successful command.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
