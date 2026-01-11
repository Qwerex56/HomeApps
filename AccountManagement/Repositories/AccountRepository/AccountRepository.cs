using AccountManagement.Data;
using AccountManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountManagement.Repositories.AccountRepository;

public class AccountRepository : IAccountRepository {
    private readonly AccountDbContext _context;

    public AccountRepository(AccountDbContext context) {
        _context = context;
    }

    public async Task<IEnumerable<Account>> GetAllAsync() {
        return await _context.Accounts
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task CreateAsync(Account item) {
        await _context.Accounts.AddAsync(item);
    }

    public async Task UpdateAsync(Account item) {
        var account = await GetByIdAsync(item.Id);

        if (account is null) {
            throw new KeyNotFoundException();
        }
        
        _context.Accounts.Update(item);
    }

    public async Task<Account?> GetByIdAsync(Guid id) {
        return await _context.Accounts.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<Account> DeleteAsync(Guid id) {
        var account = await GetByIdAsync(id);

        if (account is null) {
            throw new KeyNotFoundException();
        }

        _context.Accounts.Remove(account);
        
        return account;
    }
}