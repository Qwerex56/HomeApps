using HouseholdService.Domain.Models;
using Shared.Authorization;

namespace HouseholdService.Application.Response;

public record UserResponse(
    Guid Id,
    string Username,
    string Email,
    UserSystemRoleEnum Role
);

public static class UserResponseExtensions {
    public static UserResponse FromUser(this User user) {
        return new UserResponse(
            Id: user.Id,
            Username: user.Username,
            Email: user.Email,
            Role: user.Role
        );
    }

    public static IEnumerable<UserResponse> FromUser(this IEnumerable<User> users) {
        return users.Select(FromUser);
    }
}