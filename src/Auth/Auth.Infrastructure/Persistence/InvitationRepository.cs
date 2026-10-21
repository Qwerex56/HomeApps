using Auth.Application.Abstractions;
using Auth.Domain.Common;
using Auth.Domain.Invitations;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Persistence;

/// <summary>"Pending" is derived from the dates (<see cref="Invitation.StatusAt"/>), so the queries spell it out.</summary>
internal sealed class InvitationRepository(AuthDbContext db) : IInvitationRepository
{
    public Task<Invitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Invitations.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public Task<Invitation?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        db.Invitations.FirstOrDefaultAsync(i => i.TokenHash == tokenHash, cancellationToken);

    public Task<bool> HasPendingForAsync(Email email, DateTimeOffset now, CancellationToken cancellationToken) =>
        Pending(now).AnyAsync(i => i.Email == email, cancellationToken);

    public Task<bool> HasPendingBootstrapAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
        Pending(now).AnyAsync(i => i.Kind == InvitationKind.Bootstrap, cancellationToken);

    public async Task<IReadOnlyList<Invitation>> ListAsync(Guid? invitedBy, CancellationToken cancellationToken) =>
        await db.Invitations
            .Where(i => invitedBy == null || i.InvitedByUserId == invitedBy)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<Invitation?> GetAcceptedByAsync(Guid identityId, CancellationToken cancellationToken) =>
        db.Invitations.FirstOrDefaultAsync(i => i.AcceptedByIdentityId == identityId, cancellationToken);

    public void Add(Invitation invitation) => db.Invitations.Add(invitation);

    private IQueryable<Invitation> Pending(DateTimeOffset now) =>
        db.Invitations.Where(i => i.AcceptedAt == null && i.RevokedAt == null && i.ExpiresAt > now);
}
