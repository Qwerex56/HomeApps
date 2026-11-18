using System.Collections.Concurrent;
using HomeApps.Contracts.Households;

namespace Devices.Api.Tests;

/// <summary>Who is in which household in tests, instead of asking Households.</summary>
public sealed class HouseholdMembers
{
    private readonly ConcurrentDictionary<(Guid User, Guid Household), HouseholdRole> _roles = new();

    public void Add(Guid userId, Guid householdId, HouseholdRole role) => _roles[(userId, householdId)] = role;

    public HouseholdRole? RoleOf(Guid userId, Guid householdId) => _roles.TryGetValue((userId, householdId), out var role) ? role : null;

    public IReadOnlyList<Guid> HouseholdsOf(Guid userId) => [.. _roles.Keys.Where(k => k.User == userId).Select(k => k.Household)];
}
