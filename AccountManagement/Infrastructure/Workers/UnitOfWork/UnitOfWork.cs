using AccountService.Infrastructure.Data;

namespace AccountService.Infrastructure.Workers.UnitOfWork;

public class UnitOfWork : IUnitOfWork {
    private readonly AccountDbContext _dbContext;

    public UnitOfWork(AccountDbContext dbContext) {
        _dbContext = dbContext;
    }
    
    public async Task SaveChangesAsync() {
        await _dbContext.SaveChangesAsync();
    }

    public Task RollbackAsync() {
        throw new NotImplementedException();
    }
}