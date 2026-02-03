namespace AccountManagement.Dto.HouseholdDto;

public class CreateHouseholdDto {
    public required Guid CreatorId { get; init; }
    public required string FamilyName { get; init; }
}