using HouseholdService.Domain.Models;

namespace HouseholdService.Infrastructure.Repositories;

public interface IRepository<TEntity> where TEntity : Entity {
    public Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    public Task AddAsync(TEntity entity);
    public Task UpdateAsync(TEntity entity);
    public Task DeleteAsync(TEntity entity);
}