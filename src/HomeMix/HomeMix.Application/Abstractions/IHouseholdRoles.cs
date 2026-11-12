using HomeApps.Contracts.Households;

namespace HomeMix.Application.Abstractions;

/// <summary>Asked from Households with the caller's own token, so Households applies its own rules.</summary>
public interface IHouseholdRoles
{
    /// <summary>The caller's role in a household, or <c>null</c> without access.</summary>
    Task<HouseholdRole?> GetRoleAsync(Guid householdId, CancellationToken cancellationToken);

    /// <summary>Everyone with access to the household (guests included), as the caller may see them.</summary>
    Task<IReadOnlyList<Guid>> GetMemberIdsAsync(Guid householdId, CancellationToken cancellationToken);
}
