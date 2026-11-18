using Devices.Application.Abstractions;
using HomeApps.Contracts.Households;

namespace Devices.Application.Tests.Fakes;

internal sealed class FakeRoles(HouseholdRole? role) : IHouseholdRoles
{
    public Task<HouseholdRole?> GetRoleAsync(Guid householdId, CancellationToken cancellationToken) => Task.FromResult(role);

    public Task<IReadOnlyList<Guid>> GetMyHouseholdIdsAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
}
