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

    private readonly Dictionary<Guid, string> _names = [];

    public void Name(Guid household, string name)
    {
        lock (_lock)
        {
            _names[household] = name;
        }
    }

    /// <summary>The households a person is in, with their names (the id when none was given) and roles.</summary>
    public IReadOnlyList<(Guid Id, string Name, HouseholdRole Role)> Of(Guid user)
    {
        lock (_lock)
        {
            return [.. _roles.Where(r => r.Key.User == user)
                .Select(r => (r.Key.Household, _names.GetValueOrDefault(r.Key.Household, r.Key.Household.ToString()), r.Value))];
        }
    }
}
