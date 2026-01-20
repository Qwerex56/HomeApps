namespace AccountManagement.Dto.LoginDto;

public class RefreshTokenDto {
    public required string Token { get; init; }
    public required DateTime Expires { get; init; }
}