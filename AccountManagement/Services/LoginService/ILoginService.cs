using System.IdentityModel.Tokens.Jwt;
using AccountManagement.Dto.Credentials;
using AccountManagement.Dto.LoginDto;
using AccountManagement.Models;

namespace AccountManagement.Services.LoginService;

public interface ILoginService {
    public Task<User?> ValidateCredentials(UserCredentialsDto providedCredentialsDto);
    
    public Task<JwtSecurityToken> GenerateJwtTokenAsync(Guid userId);
    public Task<RefreshTokenDto> GenerateRefreshTokenAsync(Guid userId);
    public Task<RefreshTokenWithJwtDto> RefreshUserSession(string token);
    public Task RemoveRefreshTokenWithHash(string token);
}