using Accounts.Domain.Users;

namespace Accounts.Application.Abstractions;

/// <summary>Loads and stores the <see cref="User"/> aggregate. Saving is the unit of work's job.</summary>
public interface IUserRepository
{
    /// <summary>
    /// The user without its history: the domain only appends entries and never reads old ones, so commands do not pay
    /// for a long history (Q6). New entries are still saved.
    /// </summary>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Whether this id was erased (R7: a late <c>IdentityRegistered</c> must not bring it back).</summary>
    Task<bool> IsErasedAsync(Guid id, CancellationToken cancellationToken);

    void Add(User user);

    /// <summary>Hard delete with the history ("forget me" only, UC-17).</summary>
    void Remove(User user);

    void AddErased(ErasedUser erased);
}
