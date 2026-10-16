using AccountService.Domain.Models;

namespace AccountService.Infrastructure.Repositories;

public interface ISimpleRepository<TEntity, in TKey> : IRepository<TEntity> where TEntity : IEntity {
    public Task<TEntity?> GetByIdAsync(Guid id);
    
    public Task<TEntity> DeleteAsync(TKey id);
}