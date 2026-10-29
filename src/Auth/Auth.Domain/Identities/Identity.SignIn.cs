using Auth.Domain.Common;
using Auth.Domain.Identities.Events;
using FluentResults;

namespace Auth.Domain.Identities;

/// <summary>Signing in, refreshing and signing out (A-02 to A-06, A-09, A-14).</summary>
public sealed partial class Identity
{
    /// <summary>A-35: a refresh publishes <see cref="IdentitySignedIn"/> at most once per this period; a sign-in always does.</summary>
    public static readonly TimeSpan SignInEventThrottle = TimeSpan.FromMinutes(15);

    /// <summary>
    /// A-02. The hash is always computed (R4). A lockout or an admin-disabled account answers the same whatever the
    /// password (R3). A wrong password counts towards the lockout (R5): the handler saves even when this fails (P4).
    /// </summary>
    public Result<SignInOutcome> SignInWithPassword(string password, ClientInfo client, IPasswordHasher hasher, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(hasher);
        ArgumentNullException.ThrowIfNull(time);

        var now = time.GetUtcNow();
        var passwordMatches = hasher.Verify(PasswordHash ?? string.Empty, password ?? string.Empty) && PasswordHash is not null;

        if (Lockout.IsLocked(now))
        {
            return Result.Fail<SignInOutcome>(IdentityErrors.LockedOut);
        }

        if (Status == AccountStatus.DisabledByAdmin)
        {
            return Result.Fail<SignInOutcome>(IdentityErrors.CannotSignIn);
        }

        if (!passwordMatches)
        {
            Lockout = Lockout.RegisterFailure(now);
            Touch(now);
            return Result.Fail<SignInOutcome>(Lockout.IsLocked(now) ? IdentityErrors.LockedOut : IdentityErrors.InvalidCredentials);
        }

        if (Lockout != Lockout.None)
        {
            Lockout = Lockout.None;
            Touch(now);
        }

        return CompleteSignIn(AuthMethod.Password, client, now);
    }

    /// <summary>A-05, A-06: the handler found this identity by (provider, subject) from a verified ID token.</summary>
    public Result<SignInOutcome> SignInExternal(ExternalProvider provider, string subject, ClientInfo client, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(time);

        var now = time.GetUtcNow();
        if (!_externalLogins.Exists(l => l.Provider == provider && l.Subject == subject))
        {
            return Result.Fail<SignInOutcome>(IdentityErrors.InvalidCredentials);
        }

        if (Lockout.IsLocked(now))
        {
            return Result.Fail<SignInOutcome>(IdentityErrors.LockedOut);
        }

        if (Status == AccountStatus.DisabledByAdmin)
        {
            return Result.Fail<SignInOutcome>(IdentityErrors.CannotSignIn);
        }

        return CompleteSignIn(provider == ExternalProvider.Google ? AuthMethod.Google : AuthMethod.Apple, client, now);
    }

    /// <summary>
    /// A-09, R16: exchanges a valid reactivation ticket for a session once Accounts has enabled the account again.
    /// Until <c>UserEnabled</c> arrives the answer is <c>reactivation.pending</c> and the client retries.
    /// </summary>
    public Result<IssuedRefreshToken> CompleteReactivation(AuthMethod method, ClientInfo client, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(time);

        var now = time.GetUtcNow();
        if (Status == AccountStatus.DisabledBySelf)
        {
            return Result.Fail<IssuedRefreshToken>(IdentityErrors.ReactivationPending);
        }

        if (!CanSignIn(now))
        {
            return Result.Fail<IssuedRefreshToken>(IdentityErrors.CannotSignIn);
        }

        var token = StartSession(method, client, now);
        _domainEvents.Add(new AccountReactivatedBySelf(Id, Email));
        return token;
    }

    /// <summary>How long a just-rotated refresh token still works (R6a) unless the caller says otherwise.</summary>
    public static readonly TimeSpan DefaultRefreshGraceWindow = TimeSpan.FromSeconds(10);

    /// <summary>
    /// A-03. Single use with rotation in the same session (R6). A used token coming back, or a token presented by
    /// another client, means theft: the whole session is revoked (R7) and the handler saves despite the failure (P4).
    /// R6a: a token used no longer than <paramref name="graceWindow"/> ago, by the same client, rotates once more
    /// instead. A browser that reloads the page during a refresh never receives the successor and comes back with the
    /// old token; that is not theft.
    /// </summary>
    public Result<IssuedRefreshToken> Refresh(string rawToken, ClientInfo client, TimeProvider time, TimeSpan? graceWindow = null)
    {
        ArgumentNullException.ThrowIfNull(rawToken);
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(time);

        var now = time.GetUtcNow();
        var hash = OpaqueToken.HashOf(rawToken);
        var session = _sessions.Find(s => s.FindToken(hash) is not null);
        var token = session?.FindToken(hash);
        if (session is null || token is null || session.RevokedAt is not null)
        {
            return Result.Fail<IssuedRefreshToken>(IdentityErrors.InvalidRefreshToken);
        }

        var sameClient = session.Client.ClientId == client.ClientId;
        var window = graceWindow ?? DefaultRefreshGraceWindow;
        var inGraceWindow = window > TimeSpan.Zero && token.UsedAt is { } usedAt && now - usedAt <= window;
        if (sameClient && inGraceWindow && token.ExpiresAt > now)
        {
            return CanSignIn(now)
                ? session.IssueToken(now)
                : Result.Fail<IssuedRefreshToken>(IdentityErrors.CannotSignIn);
        }

        if (token.UsedAt is not null || !sameClient)
        {
            session.Revoke(now);
            Touch(now);
            _domainEvents.Add(new RefreshTokenReuseDetected(Id, session.Id, client.ClientId));
            return Result.Fail<IssuedRefreshToken>(IdentityErrors.InvalidRefreshToken);
        }

        if (!token.IsUsable(now))
        {
            return Result.Fail<IssuedRefreshToken>(IdentityErrors.InvalidRefreshToken);
        }

        if (!CanSignIn(now))
        {
            return Result.Fail<IssuedRefreshToken>(IdentityErrors.CannotSignIn);
        }

        token.MarkUsed(now);
        var next = session.IssueToken(now);
        if (LastSignInEventAt is null || now - LastSignInEventAt >= SignInEventThrottle)
        {
            RaiseSignedIn(null, client, now);
        }

        return next;
    }

    /// <summary>A-04, A-14: ends one session. Idempotent; an unknown id changes nothing.</summary>
    public void SignOut(Guid sessionId, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);

        var session = _sessions.Find(s => s.Id == sessionId);
        if (session is { RevokedAt: null })
        {
            var now = time.GetUtcNow();
            session.Revoke(now);
            Touch(now);
        }
    }

    /// <summary>A-14 "sign out everywhere"; also used when a password changes or the account is disabled.</summary>
    public void SignOutEverywhere(TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);
        RevokeAllSessions(time.GetUtcNow());
    }

    /// <summary>A-14: sessions that can still be refreshed, most recently used first.</summary>
    public IReadOnlyList<Session> ActiveSessions(DateTimeOffset now) =>
        [.. _sessions.Where(s => s.IsActive(now)).OrderByDescending(s => s.LastUsedAt)];

    private Result<SignInOutcome> CompleteSignIn(AuthMethod method, ClientInfo client, DateTimeOffset now)
    {
        if (Status == AccountStatus.DisabledBySelf)
        {
            return new SignInOutcome.ReactivationRequired(Id, method);
        }

        if (Status != AccountStatus.Active)
        {
            return Result.Fail<SignInOutcome>(IdentityErrors.CannotSignIn);
        }

        return new SignInOutcome.SignedIn(StartSession(method, client, now));
    }

    private IssuedRefreshToken StartSession(AuthMethod method, ClientInfo client, DateTimeOffset now)
    {
        var (session, token) = Session.Start(Id, client, method, now);
        _sessions.Add(session);
        RaiseSignedIn(method, client, now);
        Touch(now);
        return token;
    }

    private void RaiseSignedIn(AuthMethod? method, ClientInfo client, DateTimeOffset now)
    {
        LastSignInEventAt = now;
        _domainEvents.Add(new IdentitySignedIn(Id, method, client.ClientId, now));
    }

    private void RevokeAllSessions(DateTimeOffset now)
    {
        foreach (var session in _sessions.Where(s => s.RevokedAt is null))
        {
            session.Revoke(now);
        }

        Touch(now);
    }
}
