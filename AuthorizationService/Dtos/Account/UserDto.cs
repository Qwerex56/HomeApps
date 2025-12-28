namespace AuthorizationService.Dtos.Account;

public record UserDto(
    Guid Id,
    string Username,
    string Email,
    string RefreshToken
);