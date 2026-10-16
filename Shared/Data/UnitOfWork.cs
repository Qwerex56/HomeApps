using Microsoft.EntityFrameworkCore;

namespace Shared.Data;

public interface IUnitOfWork<TDbContext> where TDbContext : DbContext {
    public Task SaveChangesAsync();
}