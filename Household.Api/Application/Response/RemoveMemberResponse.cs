namespace HouseholdService.Application.Response;

public record RemoveMemberResponse(
    Guid MemberToRemoveId,
    Guid HouseholdId,
    Guid RequestedById,
    bool IsRemoved = true
);