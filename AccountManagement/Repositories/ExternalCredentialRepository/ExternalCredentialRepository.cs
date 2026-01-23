using AccountManagement.Data;
using AccountManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountManagement.Repositories.ExternalCredentialRepository;

public class ExternalCredentialRepository : IExternalCredentialRepository {
    private readonly AccountDbContext _context;

    public ExternalCredentialRepository(AccountDbContext context) {
        _context = context;
    }

    public async Task<IEnumerable<ExternalCredentials>> GetAllAsync() {
        return await _context.ExternalCredentials
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task CreateAsync(ExternalCredentials item) {
        _context.ExternalCredentials.Add(item);
    }

    public async Task UpdateAsync(ExternalCredentials item) {
        _context.ExternalCredentials.Update(item);
    }

    public async Task<ExternalCredentials> DeleteAsync(ExternalCredentials item) {
        _context.ExternalCredentials.Remove(item);
        return item;
    }

    public async Task<ExternalCredentials?> GetExternalCredentialByProviderId(string providerId) {
        var credentials = await _context.ExternalCredentials.FirstOrDefaultAsync(e => providerId == e.ProviderId);
        
        return credentials;
    }
}