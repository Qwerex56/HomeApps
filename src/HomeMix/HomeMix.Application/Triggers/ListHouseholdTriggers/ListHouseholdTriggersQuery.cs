using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Triggers.ListHouseholdTriggers;

/// <summary>The triggers of the household's add-ons with their state, for its admins.</summary>
public sealed record ListHouseholdTriggersQuery(Guid HouseholdId) : IQuery<IReadOnlyList<TriggerView>>;
