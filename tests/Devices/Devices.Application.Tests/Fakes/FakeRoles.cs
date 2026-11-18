using Devices.Application.Abstractions;
using HomeApps.Contracts.Households;

namespace Devices.Application.Tests.Fakes;

internal sealed class FakeRoles(HouseholdRole? role, params Guid[] householdIds) : IHouseholdRoles
{
    public Task<HouseholdRole?> GetRoleAsync(Guid householdId, CancellationToken cancellationToken) => Task.FromResult(role);

    public Task<IReadOnlyList<Guid>> GetMyHouseholdIdsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Guid>>(householdIds);
}
