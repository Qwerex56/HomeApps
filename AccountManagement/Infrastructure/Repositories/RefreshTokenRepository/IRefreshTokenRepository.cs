using AccountService.Domain.Models;

namespace AccountService.Infrastructure.Repositories.RefreshTokenRepository;

public interface IRefreshTokenRepository : IRepository<RefreshToken> {
    public Task<RefreshToken?> GetByUserIdAsync(Guid userId);
    public Task<RefreshToken?> GetByTokenHashAsync(string hash);
}