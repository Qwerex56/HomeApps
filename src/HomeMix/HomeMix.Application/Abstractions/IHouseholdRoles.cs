using HomeApps.Contracts.Households;

namespace HomeMix.Application.Abstractions;

/// <summary>Asked from Households with the caller's own token, so Households applies its own rules.</summary>
public interface IHouseholdRoles
{
    /// <summary>The caller's role in a household, or <c>null</c> without access.</summary>
    Task<HouseholdRole?> GetRoleAsync(Guid householdId, CancellationToken cancellationToken);

    /// <summary>Every household the caller can reach (guest access included).</summary>
    /// <summary>A household's active members, as the caller may see them; empty without access.</summary>
    Task<IReadOnlyList<HouseholdMember>> GetMembersAsync(Guid householdId, CancellationToken cancellationToken);

    Task<IReadOnlyList<MyHousehold>> GetMyHouseholdsAsync(CancellationToken cancellationToken);

    /// <summary>Everyone with access to the household (guests included), as the caller may see them.</summary>
    Task<IReadOnlyList<Guid>> GetMemberIdsAsync(Guid householdId, CancellationToken cancellationToken);
}
