using AccountManagement.Enums;

namespace AccountManagement.Dto.HouseholdDto;

public class AddUserToHouseholdRequest {
    public required Guid InviteeId { get; init; }
    public required Guid HouseholdId { get; init; }

    public required FamilyFunctionEnum FamilyFunction { get; init; } = FamilyFunctionEnum.Other;
}