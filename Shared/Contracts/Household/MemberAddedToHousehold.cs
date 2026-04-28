namespace Shared.Contracts.Household;

public record MemberAddedToHousehold(
    Guid UserId,
    Guid HouseholdId,
    DateTime AddedOn
);