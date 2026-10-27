using Households.Domain.Households;
using Households.Domain.Invites;
using Households.Domain.Users;

namespace Households.Application.Abstractions;

/// <summary>Loads and stores the <see cref="Household"/> aggregate with its memberships. Saving is the unit of work's job.</summary>
public interface IHouseholdRepository
{
    Task<Household?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>D2b: the household whose guest link has this code.</summary>
    Task<Household?> GetByGuestCodeAsync(InviteCode code, CancellationToken cancellationToken);

    /// <summary>Households where the user is a member or was the inviter of a member (ban D5, erase S4 and Q6).</summary>
    Task<IReadOnlyList<Household>> ListInvolvingAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>D2c: households with at least one guest whose access ended before <paramref name="now"/>.</summary>
    Task<IReadOnlyList<Household>> ListWithExpiredGuestsAsync(DateTimeOffset now, CancellationToken cancellationToken);

    void Add(Household household);
}

/// <summary>Loads and stores <see cref="Invite"/>.</summary>
public interface IInviteRepository
{
    Task<Invite?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Invite?> GetByCodeAsync(InviteCode code, CancellationToken cancellationToken);

    /// <summary>Invites of the household not revoked yet, optionally only those made by <paramref name="createdBy"/>.</summary>
    Task<IReadOnlyList<Invite>> ListNotRevokedAsync(Guid householdId, Guid? createdBy, CancellationToken cancellationToken);

    /// <summary>Q6: every invite this person made, in any household.</summary>
    Task<IReadOnlyList<Invite>> ListByCreatorAsync(Guid userId, CancellationToken cancellationToken);

    void Add(Invite invite);
}

/// <summary>The copy of accounts from Accounts (S1–S4).</summary>
public interface IUserCopies
{
    Task<UserCopy?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>The copies that exist among <paramref name="ids"/>; unknown ids are missing from the result.</summary>
    Task<IReadOnlyDictionary<Guid, UserCopy>> GetManyAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    void Add(UserCopy user);

    void Remove(UserCopy user);
}
