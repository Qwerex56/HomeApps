using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.Contracts.Households;
using HomeMix.Application.Abstractions;

namespace HomeMix.Api.Tests;

internal sealed class FakeHouseholdRoles(FakeHouseholds households, ICurrentUser currentUser) : IHouseholdRoles
{
    public Task<HouseholdRole?> GetRoleAsync(Guid householdId, CancellationToken cancellationToken) =>
        Task.FromResult(households.RoleOf(householdId, currentUser.RequiredUserId));

    public Task<IReadOnlyList<Guid>> GetMemberIdsAsync(Guid householdId, CancellationToken cancellationToken) =>
        Task.FromResult(households.MembersOf(householdId));
}
