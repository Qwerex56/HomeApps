using System.Collections.Concurrent;
using Jarvis.Application.Gateway;

namespace Jarvis.Api.Tests;

/// <summary>Households' assistant settings without Households: everything allowed, as a Member, unless a test says otherwise.</summary>
internal sealed class FakeCeilings : IHouseholdCeilings
{
    public ConcurrentDictionary<Guid, HouseholdCeiling> Set { get; } = new();

    public Task<HouseholdCeiling> GetAsync(Guid householdId, CancellationToken cancellationToken) =>
        Task.FromResult(Set.GetValueOrDefault(householdId, new HouseholdCeiling(true, false, true, true, true)));
}
