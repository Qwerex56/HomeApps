using AccountManagement.Models;

namespace AccountManagement.Repositories;

public interface IRepository<TEntity> where TEntity : IEntity {
    public Task<IEnumerable<TEntity>> GetAllAsync();
    
    public Task CreateAsync(TEntity item);
    
    public Task UpdateAsync(TEntity item);
    
    public Task<TEntity> DeleteAsync(TEntity item);
}