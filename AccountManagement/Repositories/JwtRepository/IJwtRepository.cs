using AccountManagement.Models;

namespace AccountManagement.Repositories.JwtRepository;

public interface IJwtRepository : IRepository<RefreshToken> {
    public Task<RefreshToken?> GetByUserIdAsync(Guid userId);
    public Task<RefreshToken?> GetByTokenHashAsync(string hash);
}