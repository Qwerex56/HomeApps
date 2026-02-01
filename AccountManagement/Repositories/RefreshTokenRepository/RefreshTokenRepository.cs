using AccountManagement.Data;
using AccountManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountManagement.Repositories.JwtRepository;

public class RefreshTokenRepository : IRefreshTokenRepository {
    private readonly AccountDbContext _context;

    public RefreshTokenRepository(AccountDbContext context) {
        _context = context;
    }

    public async Task<IEnumerable<RefreshToken>> GetAllAsync() {
        return await _context.JwtTokens
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task CreateAsync(RefreshToken item) {
        await _context.JwtTokens.AddAsync(item);
    }

    public async Task UpdateAsync(RefreshToken item) {
        _context.JwtTokens.Update(item);
    }

    public async Task<RefreshToken> DeleteAsync(RefreshToken item) {
        _context.JwtTokens.Remove(item);
        
        return item;
    }

    public async Task<RefreshToken?> GetByUserIdAsync(Guid userId) {
        var jwt = await _context.JwtTokens.FirstOrDefaultAsync(j => j.UserId == userId);
        
        return jwt;
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(string hash) {
        var refreshToken = await _context.JwtTokens
            .FirstOrDefaultAsync(t => t.TokenHash ==  hash);

        return refreshToken;
    }
}