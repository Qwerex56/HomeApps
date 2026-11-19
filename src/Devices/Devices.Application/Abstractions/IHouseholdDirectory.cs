using Devices.Domain.Devices;
using HomeApps.Contracts.Households;

namespace Devices.Application.Abstractions;

/// <summary>
/// What Devices asks Households as a service (not on behalf of a person; background jobs have no token): who the admins
/// are, and how long the household keeps events. Throws when Households cannot be reached.
/// </summary>
public interface IHouseholdDirectory
{
    /// <summary>The people who are Admin or Owner of the household now.</summary>
    Task<IReadOnlyList<Guid>> GetAdminIdsAsync(Guid householdId, CancellationToken cancellationToken);

    /// <summary>Everyone who is a member of the household now, whatever the role.</summary>
    Task<IReadOnlyList<Guid>> GetMemberIdsAsync(Guid householdId, CancellationToken cancellationToken);

    /// <summary>The person's role in the household now, or null when they are not a member.</summary>
    Task<HouseholdRole?> GetRoleAsync(Guid householdId, Guid userId, CancellationToken cancellationToken);

    /// <summary>The household's event retention (D9); <see cref="EventRetention.Default"/> when the household is gone.</summary>
    Task<EventRetention> GetEventRetentionAsync(Guid householdId, CancellationToken cancellationToken);
}
