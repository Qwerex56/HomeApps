using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.Contracts.Households;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Installations;

namespace HomeMix.Application.Installations;

/// <summary>
/// Who may do what with a household's or an account's add-ons (docs/homemix.md, "Installing"): admins and up manage a
/// household's, every member uses them; an account's are only its owner's. No access looks like nothing installed.
/// </summary>
public sealed class InstallationAccess(IHouseholdRoles households, ICurrentUser currentUser)
{
    public const HouseholdRole ManagingRole = HouseholdRole.Admin;

    public async Task<bool> CanUseAsync(InstallTarget target, CancellationToken cancellationToken) =>
        target.Kind == TargetKind.User
            ? target.Id == currentUser.RequiredUserId
            : await households.GetRoleAsync(target.Id, cancellationToken) is not null;

    public async Task<bool> CanManageAsync(InstallTarget target, CancellationToken cancellationToken) =>
        target.Kind == TargetKind.User
            ? target.Id == currentUser.RequiredUserId
            : await households.GetRoleAsync(target.Id, cancellationToken) >= ManagingRole;
}
