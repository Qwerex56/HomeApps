using Household.Api.Models;

namespace Household.Api.Repositories;

public interface IRepository<TEntity> where TEntity : Entity {
    public Task<TEntity?> GetByIdAsync(Guid id);
    public Task AddAsync(TEntity entity);
    public Task UpdateAsync(TEntity entity);
    public Task DeleteAsync(TEntity entity);
}