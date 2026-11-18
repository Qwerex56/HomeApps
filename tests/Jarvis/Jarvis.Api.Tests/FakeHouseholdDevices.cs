using System.Collections.Concurrent;
using Jarvis.Application.Abstractions;

namespace Jarvis.Api.Tests;

/// <summary>Devices' household list without Devices: empty unless a test says which devices a household has; <c>null</c> in <see cref="Down"/> means Devices did not answer.</summary>
internal sealed class FakeHouseholdDevices : IHouseholdDevices
{
    public ConcurrentDictionary<Guid, IReadOnlyList<HouseholdDevice>> Set { get; } = new();

    public HashSet<Guid> Down { get; } = [];

    /// <summary>Every call as (household, user, fresh).</summary>
    public ConcurrentQueue<(Guid Household, Guid User, bool Fresh)> Calls { get; } = new();

    /// <summary>What a fresh ask returns when it differs from the cached one (a device paired after the cache was filled).</summary>
    public ConcurrentDictionary<Guid, IReadOnlyList<HouseholdDevice>> Fresh { get; } = new();

    public Task<IReadOnlyList<HouseholdDevice>?> ListAsync(Guid householdId, Guid userId, CancellationToken cancellationToken, bool fresh = false)
    {
        Calls.Enqueue((householdId, userId, fresh));
        return fresh && Fresh.TryGetValue(householdId, out var newer)
            ? Task.FromResult<IReadOnlyList<HouseholdDevice>?>(newer)
            : Task.FromResult(Down.Contains(householdId) ? null : (IReadOnlyList<HouseholdDevice>?)Set.GetValueOrDefault(householdId, []));
    }
}
