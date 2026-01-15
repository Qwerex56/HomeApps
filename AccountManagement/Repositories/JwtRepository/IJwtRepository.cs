using AccountManagement.Models;

namespace AccountManagement.Repositories.JwtRepository;

public interface IJwtRepository : ISimpleRepository<RefreshToken, Guid> {
    public Task<RefreshToken?> GetByUserIdAsync(Guid userId);
}