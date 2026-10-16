using HouseholdService.Domain.Models;
using Shared.Authorization;

namespace HouseholdService.Application.Response;

public record UserHouseholdResponse(
    Guid Id,
    Guid UserId,
    Guid HouseholdId,
    UserFamilyRoleEnum UserFamilyRole,
    string Nickname
);

public static class UserHouseholdResponseExtension {
    public static UserHouseholdResponse ToHouseholdUserList(this UserHousehold userHousehold) {
        return new UserHouseholdResponse(
            Id: userHousehold.Id,
            UserId: userHousehold.UserId,
            HouseholdId: userHousehold.HouseholdId,
            UserFamilyRole: userHousehold.UserFamilyRole,
            Nickname: userHousehold.Nickname
        );
    }

    public static IEnumerable<UserHouseholdResponse>
        ToHouseholdUserList(this IEnumerable<UserHousehold> userHouseholds) {
        return userHouseholds.Select(ToHouseholdUserList);
    }
}