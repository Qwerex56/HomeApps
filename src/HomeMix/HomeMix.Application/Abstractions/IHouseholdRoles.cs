using HomeApps.Contracts.Households;

namespace HomeMix.Application.Abstractions;

/// <summary>The caller's role in a household, asked from Households with the caller's own token, or <c>null</c> without access.</summary>
public interface IHouseholdRoles
{
    Task<HouseholdRole?> GetRoleAsync(Guid householdId, CancellationToken cancellationToken);
}
