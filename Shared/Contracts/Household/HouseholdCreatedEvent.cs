namespace Shared.Contracts.Household;

public record HouseholdCreatedEvent(
    Guid HouseholdId,
    string Name,
    string Description
);