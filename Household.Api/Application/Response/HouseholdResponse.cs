namespace HouseholdService.Application.Response;

public record HouseholdResponse(
    Guid Id,
    string Name,
    string Description
);

public record HouseholdListResponse(
    IEnumerable<HouseholdResponse> Households,
    int Page,
    int PageSize,
    int TotalItems
);

public record HouseholdUserListResponse(
    HouseholdResponse Household,
    IEnumerable<UserHouseholdResponse> UserHouseholds
);

public static class HouseholdResponseFactory {
    public static HouseholdResponse ToHouseholdResponse(this Domain.Models.Household household) {
        return new HouseholdResponse(
            Id: household.Id,
            Name: household.Name,
            Description: household.Description
        );
    }

    public static IEnumerable<HouseholdResponse> ToResponse(this IEnumerable<Domain.Models.Household> households) {
        return households.Select(ToHouseholdResponse);
    }

    public static HouseholdUserListResponse ToHouseholdUserListResponse(Domain.Models.Household household) {
        return new HouseholdUserListResponse(
            Household: household.ToHouseholdResponse(),
            UserHouseholds: household.UserHouseholds.ToHouseholdUserList()
        );
    }
}