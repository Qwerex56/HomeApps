namespace Accounts.Domain.Users;

/// <summary>
/// The anonymous trace an erased account leaves (accounts.md 5): only the id, when and by whom. No personal data.
/// Also stops a late or repeated <c>IdentityRegistered</c> with the same id from bringing the account back (R7).
/// </summary>
public sealed class ErasedUser
{
    private ErasedUser() { }

    private ErasedUser(Guid id, DateTimeOffset erasedAt, Guid erasedBy)
    {
        Id = id;
        ErasedAt = erasedAt;
        ErasedBy = erasedBy;
    }

    public Guid Id { get; private set; }

    public DateTimeOffset ErasedAt { get; private set; }

    public Guid ErasedBy { get; private set; }

    internal static ErasedUser Create(Guid id, DateTimeOffset erasedAt, Guid erasedBy) => new(id, erasedAt, erasedBy);
}
