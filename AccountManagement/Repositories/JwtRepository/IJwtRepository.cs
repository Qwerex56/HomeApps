using AccountManagement.Models;

namespace AccountManagement.Repositories.JwtRepository;

public interface IJwtRepository : ISimpleRepository<JwtToken, Guid> {
    public Task<JwtToken?> GetByUserIdAsync(Guid userId);
}