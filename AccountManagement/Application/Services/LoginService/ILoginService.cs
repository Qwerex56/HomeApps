using AccountService.Application.Dto.Credentials;
using AccountService.Application.Dto.LoginDto;
using AccountService.Domain.Models;

namespace AccountService.Application.Services.LoginService;

public interface ILoginService {
    public Task<User?> ValidateCredentials(UserCredentialsDto providedCredentialsDto);
    
    public Task<string> GenerateJwtTokenAsync(Guid userId);
    public Task<RefreshTokenDto> GenerateRefreshTokenAsync(Guid userId);
    public Task<RefreshTokenWithJwtDto> RefreshUserSession(string token);
    public Task RemoveRefreshTokenWithHash(string token);
}