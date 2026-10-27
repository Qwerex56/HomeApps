using Accounts.Application.Abstractions;
using Accounts.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Accounts.Infrastructure.Persistence;

/// <summary>Loads the user without its history; entries the domain appends are still inserted on save (Q6).</summary>
internal sealed class UserRepository(AccountsDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<bool> IsErasedAsync(Guid id, CancellationToken cancellationToken) =>
        db.ErasedUsers.AnyAsync(e => e.Id == id, cancellationToken);

    public void Add(User user) => db.Users.Add(user);

    /// <summary>The history goes with it: the database cascades, so it does not have to be loaded first.</summary>
    public void Remove(User user) => db.Users.Remove(user);

    public void AddErased(ErasedUser erased) => db.ErasedUsers.Add(erased);
}
