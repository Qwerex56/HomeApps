namespace AccountManagement.Dto.HouseholdDto;

public class RemoveUserFromHouseholdRequest {
    public required Guid UserToRemoveId { get; init; }
    public required Guid HouseholdId { get; init; }
}