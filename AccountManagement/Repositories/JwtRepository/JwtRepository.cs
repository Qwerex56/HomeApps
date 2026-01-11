using AccountManagement.Data;
using AccountManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountManagement.Repositories.JwtRepository;

public class JwtRepository : IJwtRepository {
    private readonly AccountDbContext _context;

    public JwtRepository(AccountDbContext context) {
        _context = context;
    }

    public async Task<IEnumerable<JwtToken>> GetAllAsync() {
        return await _context.JwtTokens
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task CreateAsync(JwtToken item) {
        await _context.JwtTokens.AddAsync(item);
    }

    public async Task UpdateAsync(JwtToken item) {
        var jwt = await GetByIdAsync(item.Id);

        if (jwt is null) {
            throw new KeyNotFoundException();
        }
        
        _context.JwtTokens.Update(item);
    }

    public async Task<JwtToken?> GetByIdAsync(Guid id) {
        var jwt = await _context.JwtTokens.FirstOrDefaultAsync(j => j.Id == id);
        
        return jwt;
    }

    public async Task<JwtToken> DeleteAsync(Guid id) {
        var jwt = await GetByIdAsync(id);

        if (jwt is null) {
            throw new KeyNotFoundException();
        }
        
        _context.JwtTokens.Remove(jwt);
        
        return jwt;
    }

    public async Task<JwtToken?> GetByUserIdAsync(Guid userId) {
        var jwt = await _context.JwtTokens.FirstOrDefaultAsync(j => j.UserId == userId);
        
        return jwt;
    }
}