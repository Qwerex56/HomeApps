using AccountManagement.Models;

namespace AccountManagement.Repositories;

public interface ICompositeRepository<TEntity, in TKey1, in TKey2> : IRepository<TEntity> where TEntity : IEntity {
    public Task<TEntity?> GetByIdAsync(TKey1 key1, TKey2 key2);
    
    public Task<TEntity> DeleteAsync(TKey1 key1, TKey2 key2);
}

public interface ICompositeRepository<TEntity, in TKey1, in TKey2, in TKey3> : IRepository<TEntity>
    where TEntity : IEntity {
    public Task<TEntity?> GetByIdAsync(TKey1 key1, TKey2 key2, TKey3 key3);
    
    public Task<TEntity> DeleteAsync(TKey1 key1, TKey2 key2, TKey3 key3);
}