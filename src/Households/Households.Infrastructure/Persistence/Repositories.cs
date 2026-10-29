using Households.Application.Abstractions;
using Households.Domain.Households;
using Households.Domain.Invites;
using Households.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Households.Infrastructure.Persistence;

/// <summary>Households always come with their memberships (auto-included); at most 100 rows each (D10).</summary>
internal sealed class HouseholdRepository(HouseholdsDbContext db) : IHouseholdRepository
{
    public Task<Household?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Households.FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

    public Task<Household?> GetByGuestCodeAsync(InviteCode code, CancellationToken cancellationToken) =>
        db.Households.FirstOrDefaultAsync(h => h.GuestCode == code, cancellationToken);

    public async Task<IReadOnlyList<Household>> ListInvolvingAsync(Guid userId, CancellationToken cancellationToken) =>
        await db.Households
            .Where(h => db.Memberships.Any(m => m.HouseholdId == h.Id && (m.UserId == userId || m.InvitedBy == userId)))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Household>> ListWithExpiredGuestsAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
        await db.Households
            .Where(h => db.Memberships.Any(m => m.HouseholdId == h.Id && m.AccessUntil != null && m.AccessUntil <= now))
            .ToListAsync(cancellationToken);

    public void Add(Household household) => db.Households.Add(household);
}

internal sealed class InviteRepository(HouseholdsDbContext db) : IInviteRepository
{
    public Task<Invite?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Invites.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public Task<Invite?> GetByCodeAsync(InviteCode code, CancellationToken cancellationToken) =>
        db.Invites.FirstOrDefaultAsync(i => i.Code == code, cancellationToken);

    public async Task<IReadOnlyList<Invite>> ListNotRevokedAsync(Guid householdId, Guid? createdBy, CancellationToken cancellationToken)
    {
        var invites = db.Invites.Where(i => i.HouseholdId == householdId && i.RevokedAt == null);
        if (createdBy is { } author)
        {
            invites = invites.Where(i => i.CreatedBy == author);
        }

        return await invites.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Invite>> ListByCreatorAsync(Guid userId, CancellationToken cancellationToken) =>
        await db.Invites.Where(i => i.CreatedBy == userId).ToListAsync(cancellationToken);

    public void Add(Invite invite) => db.Invites.Add(invite);
}

internal sealed class UserCopies(HouseholdsDbContext db) : IUserCopies
{
    public Task<UserCopy?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, UserCopy>> GetManyAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ids);
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, UserCopy>();
        }

        return await db.Users.Where(u => ids.Contains(u.Id)).ToDictionaryAsync(u => u.Id, cancellationToken);
    }

    public void Add(UserCopy user) => db.Users.Add(user);

    public void Remove(UserCopy user) => db.Users.Remove(user);
}
