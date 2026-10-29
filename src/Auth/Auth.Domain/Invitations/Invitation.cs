using Auth.Domain.Common;
using Auth.Domain.Invitations.Events;
using FluentResults;
using HomeApps.BuildingBlocks.Domain;

namespace Auth.Domain.Invitations;

/// <summary>
/// An invitation to the app sent to an email address (auth.md 2.2). The identity is created only when it is accepted;
/// until then nothing exists "on hold".
/// </summary>
public sealed class Invitation : IHasDomainEvents
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    private readonly List<IDomainEvent> _domainEvents = [];

    private Invitation() { }

    private Invitation(Guid id, Email email, string tokenHash, InvitationKind kind, Inviter? inviter, DateTimeOffset now)
    {
        Id = id;
        Email = email;
        TokenHash = tokenHash;
        Code = InvitationCode.New();
        Kind = kind;
        InvitedByUserId = inviter?.UserId;
        InvitedByEmail = inviter?.Email;
        CreatedAt = now;
        ExpiresAt = now + Lifetime;
    }

    public Guid Id { get; private set; }

    /// <summary>Where the invitation was sent. Cleared only if the person who accepted it is erased (P7).</summary>
    public Email? Email { get; private set; }

    public string TokenHash { get; private set; } = null!;

    public InvitationCode Code { get; private set; } = null!;

    public InvitationKind Kind { get; private set; }

    public Guid? InvitedByUserId { get; private set; }

    /// <summary>
    /// The inviter's email when the invitation was sent: a fact about this invitation (audit, P7),
    /// so it stays even if the inviter later changes their email or is erased.
    /// </summary>
    public Email? InvitedByEmail { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? AcceptedAt { get; private set; }

    public Guid? AcceptedByIdentityId { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents;

    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>A-20. R10: any active account may invite. R14: the email has no identity and no pending invitation.</summary>
    public static Result<IssuedInvitation> Create(
        Inviter inviter, Email email, bool emailHasIdentity, bool hasPendingInvitation, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(inviter);
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(time);

        if (!inviter.CanSignIn)
        {
            return Result.Fail<IssuedInvitation>(InvitationErrors.InviterCannotSignIn);
        }

        if (emailHasIdentity)
        {
            return Result.Fail<IssuedInvitation>(Identities.IdentityErrors.EmailTaken);
        }

        if (hasPendingInvitation)
        {
            return Result.Fail<IssuedInvitation>(InvitationErrors.AlreadyPending);
        }

        return Issue(email, InvitationKind.Invite, inviter, time.GetUtcNow());
    }

    /// <summary>A-30: the invitation for the first Owner, from configuration.</summary>
    public static IssuedInvitation CreateBootstrap(Email email, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(time);

        return Issue(email, InvitationKind.Bootstrap, null, time.GetUtcNow());
    }

    public InvitationStatus StatusAt(DateTimeOffset now) => this switch
    {
        { AcceptedAt: not null } => InvitationStatus.Accepted,
        { RevokedAt: not null } => InvitationStatus.Revoked,
        _ when ExpiresAt <= now => InvitationStatus.Expired,
        _ => InvitationStatus.Pending,
    };

    public bool Matches(string rawToken) => TokenHash == OpaqueToken.HashOf(rawToken);

    /// <summary>
    /// A-21: a new link and a new 7 days; the old link stops working. Pending or expired only (Q2). The inviter or an
    /// Admin/Owner may do it (R10). The bootstrap invitation renews only on service start (A-30).
    /// </summary>
    public Result<string> Resend(Guid actorId, ServerRole actorRole, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);

        var now = time.GetUtcNow();
        var allowed = CheckManagedBy(actorId, actorRole);
        if (allowed.IsFailed)
        {
            return allowed;
        }

        if (StatusAt(now) is not (InvitationStatus.Pending or InvitationStatus.Expired) || Email is null)
        {
            return Result.Fail<string>(InvitationErrors.InvalidOrExpired);
        }

        var (raw, hash) = OpaqueToken.New();
        TokenHash = hash;
        ExpiresAt = now + Lifetime;
        _domainEvents.Add(new InvitationCreated(Id, Email, raw, Code, Kind, InvitedByEmail, ExpiresAt));
        return raw;
    }

    /// <summary>A-21: only a pending invitation; the link stops working. Same permissions as <see cref="Resend"/>.</summary>
    public Result Revoke(Guid actorId, ServerRole actorRole, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);

        var now = time.GetUtcNow();
        var allowed = CheckManagedBy(actorId, actorRole);
        if (allowed.IsFailed)
        {
            return allowed;
        }

        if (StatusAt(now) != InvitationStatus.Pending)
        {
            return Result.Fail(InvitationErrors.InvalidOrExpired);
        }

        RevokedAt = now;
        return Result.Ok();
    }

    /// <summary>A-32, P7: the person who accepted this invitation was erased; forget the address it was sent to.</summary>
    public void ForgetInvitee() => Email = null;

    /// <summary>R14: single use, only while pending. Called by <c>Identity</c> registration only (P2).</summary>
    internal Result Accept(Guid identityId, DateTimeOffset now)
    {
        if (StatusAt(now) != InvitationStatus.Pending || Email is null)
        {
            return Result.Fail(InvitationErrors.InvalidOrExpired);
        }

        AcceptedAt = now;
        AcceptedByIdentityId = identityId;
        return Result.Ok();
    }

    private Result CheckManagedBy(Guid actorId, ServerRole actorRole) =>
        Kind == InvitationKind.Invite && (InvitedByUserId == actorId || actorRole >= ServerRole.Admin)
            ? Result.Ok()
            : Result.Fail(Identities.IdentityErrors.ForbiddenRole);

    private static IssuedInvitation Issue(Email email, InvitationKind kind, Inviter? inviter, DateTimeOffset now)
    {
        var (raw, hash) = OpaqueToken.New();
        var invitation = new Invitation(Guid.CreateVersion7(now), email, hash, kind, inviter, now);
        invitation._domainEvents.Add(new InvitationCreated(
            invitation.Id, email, raw, invitation.Code, kind, invitation.InvitedByEmail, invitation.ExpiresAt));
        return new IssuedInvitation(invitation, raw);
    }
}
