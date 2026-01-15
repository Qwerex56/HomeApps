using AccountManagement.Data;
using AccountManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountManagement.Repositories.JwtRepository;

public class JwtRepository : IJwtRepository {
    private readonly AccountDbContext _context;

    public JwtRepository(AccountDbContext context) {
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
        var jwt = await GetByIdAsync(item.Id);

        if (jwt is null) {
            throw new KeyNotFoundException();
        }
        
        _context.JwtTokens.Update(item);
    }

    public async Task<RefreshToken?> GetByIdAsync(Guid id) {
        var jwt = await _context.JwtTokens.FirstOrDefaultAsync(j => j.Id == id);
        
        return jwt;
    }

    public async Task<RefreshToken> DeleteAsync(Guid id) {
        var jwt = await GetByIdAsync(id);

        if (jwt is null) {
            throw new KeyNotFoundException();
        }
        
        _context.JwtTokens.Remove(jwt);
        
        return jwt;
    }

    public async Task<RefreshToken?> GetByUserIdAsync(Guid userId) {
        var jwt = await _context.JwtTokens.FirstOrDefaultAsync(j => j.UserId == userId);
        
        return jwt;
    }
}