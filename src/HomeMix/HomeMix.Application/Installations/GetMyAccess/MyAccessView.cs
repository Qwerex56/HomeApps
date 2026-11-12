using HomeApps.Contracts.Households;

namespace HomeMix.Application.Installations.GetMyAccess;

/// <param name="Role">The caller's household role, or <c>null</c> on their own account.</param>
/// <param name="Permissions">The add-on's permissions the caller has, which the bridge's <c>can()</c> answers from.</param>
public sealed record MyAccessView(HouseholdRole? Role, IReadOnlyList<string> Permissions);
