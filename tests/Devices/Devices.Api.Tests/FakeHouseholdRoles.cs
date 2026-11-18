using Devices.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.Contracts.Households;

namespace Devices.Api.Tests;

internal sealed class FakeHouseholdRoles(HouseholdMembers members, ICurrentUser currentUser) : IHouseholdRoles
{
    public Task<HouseholdRole?> GetRoleAsync(Guid householdId, CancellationToken cancellationToken) =>
        Task.FromResult(members.RoleOf(currentUser.RequiredUserId, householdId));

    public Task<IReadOnlyList<Guid>> GetMyHouseholdIdsAsync(CancellationToken cancellationToken) =>
        Task.FromResult(members.HouseholdsOf(currentUser.RequiredUserId));
}
