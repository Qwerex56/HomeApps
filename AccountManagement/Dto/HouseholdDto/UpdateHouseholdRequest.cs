namespace AccountManagement.Dto.HouseholdDto;

public class UpdateHouseholdRequest {
    public required Guid HouseholdId { get; init; }
    public required string FamilyName { get; init; }
}