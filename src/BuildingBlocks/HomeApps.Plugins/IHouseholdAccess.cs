using HomeApps.Contracts.Households;

namespace HomeApps.Plugins;

/// <summary>
/// The caller's membership in a household, asked from Households (variant K2 in jarvis-hub.md 9b.6): plugins keep no
/// copy of who is in which household. Answers are cached briefly and dropped at once when Households reports a change.
/// </summary>
public interface IHouseholdAccess
{
    /// <summary>
    /// The current user's membership, or <c>null</c> when they have no access: not a member, the household is archived
    /// or does not exist, or a guest's access ended. Works inside a request with the user's access token, including a
    /// connection to the plugin's realtime hub (the token from <c>?access_token=</c>).
    /// </summary>
    Task<HouseholdMembership?> GetMembershipAsync(Guid householdId, CancellationToken cancellationToken);

    /// <summary>Only the role from <see cref="GetMembershipAsync"/>.</summary>
    async Task<HouseholdRole?> GetRoleAsync(Guid householdId, CancellationToken cancellationToken) =>
        (await GetMembershipAsync(householdId, cancellationToken).ConfigureAwait(false))?.Role;
}
