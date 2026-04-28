namespace AccountService.Application.Dto.LoginDto;

public class RefreshTokenWithJwtDto {
    public required RefreshTokenDto RefreshToken { get; init; }
    public required string JwtToken { get; init; }
}