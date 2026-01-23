namespace AccountManagement.Workers.UnitOfWork;

public interface IUnitOfWork {
    public Task SaveChangesAsync();
    public Task RollbackAsync();
}