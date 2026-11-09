using Notifications.Domain.Recipients;

namespace Notifications.Application.Abstractions;

/// <summary>Recipients with their devices and mutes; changes are saved by the unit of work after the command.</summary>
public interface IRecipientRepository
{
    Task<Recipient?> FindAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Those of <paramref name="userIds"/> that exist; people who never turned notifications on are left out.</summary>
    Task<IReadOnlyList<Recipient>> GetManyAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken);

    /// <summary>Recipients that have a device with this endpoint (another person signed in on the same browser).</summary>
    Task<IReadOnlyList<Recipient>> FindByEndpointAsync(string endpoint, CancellationToken cancellationToken);

    void Add(Recipient recipient);

    void Remove(Recipient recipient);
}
