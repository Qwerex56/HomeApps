using Auth.Domain.Common;
using Auth.Domain.Identities.Events;
using Auth.Domain.Invitations;
using FluentResults;
using HomeApps.BuildingBlocks.Domain;

namespace Auth.Domain.Identities;

/// <summary>
/// Who a user is and how they prove it (auth.md 2.1): email, password, external logins, sessions and the server role.
/// The profile lives in Accounts. Created only through the Register* factories, always before the Accounts user.
/// </summary>
public sealed partial class Identity : IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];
    private readonly List<ExternalLogin> _externalLogins = [];
    private readonly List<Session> _sessions = [];

    private Identity() { }

    private Identity(Guid id, Email email, string? passwordHash, ServerRole role, RegistrationSource source, DateTimeOffset now)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        Status = AccountStatus.Active;
        Source = source;
        RegisteredAt = now;
        UpdatedAt = now;
    }

    public Guid Id { get; private set; }

    public Email Email { get; private set; } = null!;

    /// <summary>Null for an identity that signs in only with Google or Apple.</summary>
    public string? PasswordHash { get; private set; }

    public ServerRole Role { get; private set; }

    public AccountStatus Status { get; private set; }

    public RegistrationSource Source { get; private set; }

    public Lockout Lockout { get; private set; } = Lockout.None;

    public EmailToken? EmailToken { get; private set; }

    /// <summary>When <c>IdentitySignedIn</c> was last published; throttles it to one per 15 minutes (A-35).</summary>
    public DateTimeOffset? LastSignInEventAt { get; private set; }

    public DateTimeOffset RegisteredAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyList<ExternalLogin> ExternalLogins => _externalLogins;

    public IReadOnlyList<Session> Sessions => _sessions;

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents;

    public bool HasPassword => PasswordHash is not null;

    /// <summary>R3: active and not locked out.</summary>
    public bool CanSignIn(DateTimeOffset now) => Status == AccountStatus.Active && !Lockout.IsLocked(now);

    /// <summary>R16: disabled by the user themselves and not locked out; a valid sign-in then only yields a reactivation ticket.</summary>
    public bool CanReactivate(DateTimeOffset now) => Status == AccountStatus.DisabledBySelf && !Lockout.IsLocked(now);

    public void ClearDomainEvents() => _domainEvents.Clear();

    private void Touch(DateTimeOffset now) => UpdatedAt = now;

    /// <summary>A-01: sign-up with a password, without an invitation. R1, R2, R15.</summary>
    public static Result<Registration> RegisterWithPassword(
        Email email,
        string password,
        RegistrationProfile profile,
        RegistrationContext context,
        ClientInfo client,
        bool emailTaken,
        bool registrationOpen,
        IPasswordHasher hasher,
        TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(hasher);
        ArgumentNullException.ThrowIfNull(time);

        if (!registrationOpen)
        {
            return Result.Fail<Registration>(IdentityErrors.RegistrationClosed);
        }

        return RegisterPassword(email, password, profile, context, client, null, emailTaken, hasher, time.GetUtcNow());
    }

    /// <summary>
    /// A-07 (a) and A-30: sign-up with a password through an invitation. The email is the invited one (R14: having the
    /// link proves access to the mailbox). Works even when open registration is off. Accepts the invitation (P2).
    /// </summary>
    public static Result<Registration> AcceptInvitationWithPassword(
        Invitation invitation,
        string password,
        RegistrationProfile profile,
        RegistrationContext context,
        ClientInfo client,
        bool emailTaken,
        IPasswordHasher hasher,
        TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(invitation);
        ArgumentNullException.ThrowIfNull(hasher);
        ArgumentNullException.ThrowIfNull(time);

        var now = time.GetUtcNow();
        if (invitation.StatusAt(now) != InvitationStatus.Pending || invitation.Email is null)
        {
            return Result.Fail<Registration>(InvitationErrors.InvalidOrExpired);
        }

        return RegisterPassword(invitation.Email, password, profile, context, client, invitation, emailTaken, hasher, now);
    }

    /// <summary>
    /// A-05, A-06, A-07 (b): first sign-in with Google or Apple. The email is the one the provider verified, which may
    /// differ from the invited one (R14, D8). R1 and R11 are checked by the handler and passed in; when the email is
    /// taken the handler starts the linking flow (A-15) instead of calling this.
    /// </summary>
    public static Result<Registration> RegisterExternal(
        ExternalProvider provider,
        string subject,
        Email email,
        RegistrationProfile profile,
        RegistrationContext context,
        ClientInfo client,
        Invitation? invitation,
        bool emailTaken,
        bool loginTaken,
        bool registrationOpen,
        TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(time);

        var now = time.GetUtcNow();
        if (invitation is null && !registrationOpen)
        {
            return Result.Fail<Registration>(IdentityErrors.RegistrationClosed);
        }

        if (emailTaken)
        {
            return Result.Fail<Registration>(IdentityErrors.EmailTaken);
        }

        if (loginTaken)
        {
            return Result.Fail<Registration>(IdentityErrors.ExternalLoginAlreadyLinked);
        }

        var login = ExternalLogin.Create(provider, subject, email, now);
        if (login.IsFailed)
        {
            return login.ToResult<Registration>();
        }

        var source = provider == ExternalProvider.Google ? RegistrationSource.Google : RegistrationSource.Apple;
        var method = provider == ExternalProvider.Google ? AuthMethod.Google : AuthMethod.Apple;
        return Register(email, null, login.Value, source, method, profile, context, client, invitation, now);
    }

    private static Result<Registration> RegisterPassword(
        Email email,
        string password,
        RegistrationProfile profile,
        RegistrationContext context,
        ClientInfo client,
        Invitation? invitation,
        bool emailTaken,
        IPasswordHasher hasher,
        DateTimeOffset now)
    {
        if (emailTaken)
        {
            return Result.Fail<Registration>(IdentityErrors.EmailTaken);
        }

        var policy = PasswordPolicy.Validate(password, email);
        if (policy.IsFailed)
        {
            return policy.ToResult<Registration>();
        }

        return Register(
            email, hasher.Hash(password), null, RegistrationSource.Password, AuthMethod.Password,
            profile, context, client, invitation, now);
    }

    private static Result<Registration> Register(
        Email email,
        string? passwordHash,
        ExternalLogin? login,
        RegistrationSource source,
        AuthMethod method,
        RegistrationProfile profile,
        RegistrationContext context,
        ClientInfo client,
        Invitation? invitation,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(client);

        var bootstrap = invitation?.Kind == InvitationKind.Bootstrap;
        var identity = new Identity(
            Guid.CreateVersion7(now),
            email,
            passwordHash,
            bootstrap ? ServerRole.Owner : ServerRole.User,
            bootstrap ? RegistrationSource.Bootstrap : source,
            now);

        if (invitation is not null)
        {
            var accepted = invitation.Accept(identity.Id, now);
            if (accepted.IsFailed)
            {
                return accepted.ToResult<Registration>();
            }
        }

        if (login is not null)
        {
            identity._externalLogins.Add(login);
        }

        var (session, token) = Session.Start(identity.Id, client, method, now);
        identity._sessions.Add(session);

        identity._domainEvents.Add(new IdentityRegistered(
            identity.Id,
            email,
            identity.Role,
            identity.Source,
            profile,
            context,
            invitation?.InvitedByEmail,
            invitation?.Email,
            now));

        return new Registration(identity, token);
    }
}
