using System.IdentityModel.Tokens.Jwt;

namespace AccountManagement.Dto.LoginDto;

public class RefreshTokenWithJwtDto {
    public required RefreshTokenDto RefreshToken { get; init; }
    public required string JwtToken { get; init; }
}