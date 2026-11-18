using System.Collections.Concurrent;
using Jarvis.Application.Abstractions;

namespace Jarvis.Api.Tests;

/// <summary>Devices' household list without Devices: empty unless a test says which devices a household has; <c>null</c> in <see cref="Down"/> means Devices did not answer.</summary>
internal sealed class FakeHouseholdDevices : IHouseholdDevices
{
    public ConcurrentDictionary<Guid, IReadOnlyList<HouseholdDevice>> Set { get; } = new();

    public HashSet<Guid> Down { get; } = [];

    public Task<IReadOnlyList<HouseholdDevice>?> ListAsync(Guid householdId, CancellationToken cancellationToken) =>
        Task.FromResult(Down.Contains(householdId) ? null : (IReadOnlyList<HouseholdDevice>?)Set.GetValueOrDefault(householdId, []));
}
