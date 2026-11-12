using HomeApps.Contracts.Households;

namespace HomeMix.Api.Tests;

/// <summary>Who is in which household with what role, instead of asking the Households service.</summary>
public sealed class FakeHouseholds
{
    private readonly Dictionary<(Guid Household, Guid User), HouseholdRole> _roles = [];
    private readonly Lock _lock = new();

    public void Set(Guid household, Guid user, HouseholdRole role)
    {
        lock (_lock)
        {
            _roles[(household, user)] = role;
        }
    }

    public HouseholdRole? RoleOf(Guid household, Guid user)
    {
        lock (_lock)
        {
            return _roles.TryGetValue((household, user), out var role) ? role : null;
        }
    }

    public IReadOnlyList<Guid> MembersOf(Guid household)
    {
        lock (_lock)
        {
            return [.. _roles.Keys.Where(k => k.Household == household).Select(k => k.User)];
        }
    }
}
