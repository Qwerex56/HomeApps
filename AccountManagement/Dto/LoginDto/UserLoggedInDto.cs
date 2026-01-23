using System.IdentityModel.Tokens.Jwt;

namespace AccountManagement.Dto.LoginDto;

public class UserLoggedInDto {
    public required Guid UserId { get; init; }
    public required string DisplayName { get; init; }
    public required string EmailAddress { get; init; }
    
    public required JwtSecurityToken Token { get; init; }
}