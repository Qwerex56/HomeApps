using HomeApps.Contracts.Households;

namespace Devices.Application.Abstractions;

/// <summary>Households asked with the caller's own token, so its rules (archived, guest access ended) apply.</summary>
public interface IHouseholdRoles
{
    /// <summary>The caller's role in the household, or null when the caller is not (or no longer) a member.</summary>
    Task<HouseholdRole?> GetRoleAsync(Guid householdId, CancellationToken cancellationToken);

    /// <summary>The households the caller is a member of now.</summary>
    Task<IReadOnlyList<Guid>> GetMyHouseholdIdsAsync(CancellationToken cancellationToken);
}
