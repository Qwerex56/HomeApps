namespace AccountManagement.Dto.HouseholdDto;

public class UpdateHouseholdDto {
    public required Guid HouseholdId { get; init; }
    public required Guid UserId { get; init; }
    
    public required string FamilyName { get; init; }
}