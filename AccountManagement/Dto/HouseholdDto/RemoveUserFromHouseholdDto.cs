namespace AccountManagement.Dto.HouseholdDto;

public class RemoveUserFromHouseholdDto {
    public required Guid RemoverId { get; init; }
    public required Guid UserToRemoveId { get; init; }
    public required Guid HouseholdId { get; init; }
}