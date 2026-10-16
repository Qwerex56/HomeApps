using TodoListService.Domain.Repositories;

namespace TodoListService.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork {
    private readonly DbContext _context;

    private IUserRepository _userRepository;
    private ITodoListRepository _todoListRepository;
    private ITodoItemRepository _todoItemRepository;

    public UnitOfWork(DbContext context,
                      IUserRepository userRepository,
                      ITodoListRepository todoListRepository,
                      ITodoItemRepository todoItemRepository) {
        _context = context;
        _userRepository = userRepository;
        _todoListRepository = todoListRepository;
        _todoItemRepository = todoItemRepository;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default) {
        if (!_context.Database.CanConnect())
        {
            throw new InvalidOperationException("DbContext is not connected to a database.");
        }

        _context.Database.BeginTransaction();
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default) {
        if (!_context.Database.CanConnect())
        {
            throw new InvalidOperationException("DbContext is database disconnected.");
        }

        await _context.SaveChangesAsync(cancellationToken);
        _context.Database.CommitTransaction();
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default) {
        throw new NotImplementedException();
    }

    public void Dispose() {
        _context?.Dispose();
    }

    public async ValueTask DisposeAsync() {
        await _context?.DisposeAsync();
    }
}
}