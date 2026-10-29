using Auth.Domain.Common;
using Auth.Domain.Identities.Events;
using FluentResults;

namespace Auth.Domain.Identities;

/// <summary>
/// Password, email and external logins (A-08, A-11 to A-13, A-22). Every change of password or email is only a
/// request until the link from the email is used; the newest request replaces any older one (R9, P8).
/// </summary>
public sealed partial class Identity
{
    /// <summary>R8: how recent a Google/Apple sign-in must be to change credentials of an identity without a password.</summary>
    public static readonly TimeSpan ReauthenticationWindow = TimeSpan.FromMinutes(5);

    /// <summary>A-08. The handler answers 202 whether or not the email has an identity (R9).</summary>
    public void RequestPasswordReset(TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);

        var now = time.GetUtcNow();
        var (token, raw) = EmailToken.ForPasswordReset(now);
        EmailToken = token;
        Touch(now);
        _domainEvents.Add(new PasswordResetRequested(Id, Email, raw));
    }

    /// <summary>A-22, R10: only a higher role may ask; also ends every session of this identity (a lost phone).</summary>
    public Result RequestPasswordResetBy(ServerRole actorRole, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);

        if (actorRole <= Role)
        {
            return Result.Fail(IdentityErrors.ForbiddenRole);
        }

        RevokeAllSessions(time.GetUtcNow());
        RequestPasswordReset(time);
        return Result.Ok();
    }

    /// <summary>A-08: sets the new password from the link, lifts the lockout and ends every session.</summary>
    public Result ResetPassword(string rawToken, string newPassword, IPasswordHasher hasher, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(hasher);
        ArgumentNullException.ThrowIfNull(time);

        var now = time.GetUtcNow();
        var token = MatchingEmailToken(rawToken, EmailTokenPurpose.PasswordReset, now);
        if (token is null)
        {
            return Result.Fail(IdentityErrors.TokenInvalidOrExpired);
        }

        var policy = PasswordPolicy.Validate(newPassword, Email);
        if (policy.IsFailed)
        {
            return policy;
        }

        PasswordHash = hasher.Hash(newPassword);
        Lockout = Lockout.None;
        EmailToken = null;
        RevokeAllSessions(now);
        return Result.Ok();
    }

    /// <summary>
    /// A-12. R8: the current password, or for an identity without one a sign-in within the last 5 minutes. R2.
    /// Nothing changes yet: the new password waits, hashed, until the link is used.
    /// </summary>
    public Result RequestPasswordChange(
        string? currentPassword, string newPassword, DateTimeOffset authTime, IPasswordHasher hasher, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(hasher);
        ArgumentNullException.ThrowIfNull(time);

        var now = time.GetUtcNow();
        var proof = ProveIdentity(currentPassword, authTime, hasher, now);
        if (proof.IsFailed)
        {
            return proof;
        }

        var policy = PasswordPolicy.Validate(newPassword, Email);
        if (policy.IsFailed)
        {
            return policy;
        }

        var (token, raw) = EmailToken.ForPasswordChange(hasher.Hash(newPassword), now);
        EmailToken = token;
        Touch(now);
        _domainEvents.Add(new PasswordChangeRequested(Id, Email, raw));
        return Result.Ok();
    }

    /// <summary>A-12: the link sets the waiting password and ends every session (Q1).</summary>
    public Result ConfirmPasswordChange(string rawToken, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);

        var now = time.GetUtcNow();
        var token = MatchingEmailToken(rawToken, EmailTokenPurpose.PasswordChange, now);
        if (token?.NewPasswordHash is null)
        {
            return Result.Fail(IdentityErrors.TokenInvalidOrExpired);
        }

        PasswordHash = token.NewPasswordHash;
        EmailToken = null;
        RevokeAllSessions(now);
        return Result.Ok();
    }

    /// <summary>A-13. R8 as for a password change; R1 checked by the handler. The link goes to the new address.</summary>
    public Result RequestEmailChange(
        string? currentPassword, DateTimeOffset authTime, Email newEmail, bool emailTaken, IPasswordHasher hasher, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(newEmail);
        ArgumentNullException.ThrowIfNull(hasher);
        ArgumentNullException.ThrowIfNull(time);

        var now = time.GetUtcNow();
        var proof = ProveIdentity(currentPassword, authTime, hasher, now);
        if (proof.IsFailed)
        {
            return proof;
        }

        if (newEmail == Email)
        {
            return Result.Fail(IdentityErrors.EmailUnchanged);
        }

        if (emailTaken)
        {
            return Result.Fail(IdentityErrors.EmailTaken);
        }

        var (token, raw) = EmailToken.ForEmailChange(newEmail, now);
        EmailToken = token;
        Touch(now);
        _domainEvents.Add(new EmailChangeRequested(Id, newEmail, raw));
        return Result.Ok();
    }

    /// <summary>A-13: R1 is checked again, because the address may have been taken since the request.</summary>
    public Result ConfirmEmailChange(string rawToken, bool emailTaken, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);

        var now = time.GetUtcNow();
        var token = MatchingEmailToken(rawToken, EmailTokenPurpose.EmailChange, now);
        if (token?.NewEmail is null)
        {
            return Result.Fail(IdentityErrors.TokenInvalidOrExpired);
        }

        if (emailTaken)
        {
            return Result.Fail(IdentityErrors.EmailTaken);
        }

        Email = token.NewEmail;
        EmailToken = null;
        Touch(now);
        _domainEvents.Add(new IdentityEmailChanged(Id, Email));
        return Result.Ok();
    }

    /// <summary>A-11, A-15. R11: the login is free across Auth, and this identity has none from that provider yet.</summary>
    public Result LinkExternalLogin(ExternalProvider provider, string subject, Email email, bool loginTaken, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(time);

        if (loginTaken || _externalLogins.Exists(l => l.Provider == provider))
        {
            return Result.Fail(IdentityErrors.ExternalLoginAlreadyLinked);
        }

        var now = time.GetUtcNow();
        var login = ExternalLogin.Create(provider, subject, email, now);
        if (login.IsFailed)
        {
            return login.ToResult();
        }

        _externalLogins.Add(login.Value);
        Touch(now);
        return Result.Ok();
    }

    /// <summary>A-11, R12: the last way of signing in cannot be removed. Unlinking a provider that is not linked is a no-op.</summary>
    public Result UnlinkExternalLogin(ExternalProvider provider, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);

        var login = _externalLogins.Find(l => l.Provider == provider);
        if (login is null)
        {
            return Result.Ok();
        }

        if (!HasPassword && _externalLogins.Count == 1)
        {
            return Result.Fail(IdentityErrors.LastSignInMethod);
        }

        _externalLogins.Remove(login);
        Touch(time.GetUtcNow());
        return Result.Ok();
    }

    private Result ProveIdentity(string? currentPassword, DateTimeOffset authTime, IPasswordHasher hasher, DateTimeOffset now)
    {
        if (PasswordHash is not null)
        {
            return currentPassword is not null && hasher.Verify(PasswordHash, currentPassword)
                ? Result.Ok()
                : Result.Fail(IdentityErrors.InvalidCredentials);
        }

        return now - authTime <= ReauthenticationWindow
            ? Result.Ok()
            : Result.Fail(IdentityErrors.ReauthenticationRequired);
    }

    private EmailToken? MatchingEmailToken(string rawToken, EmailTokenPurpose purpose, DateTimeOffset now) =>
        rawToken is not null && EmailToken is not null && EmailToken.Matches(rawToken, purpose, now) ? EmailToken : null;
}
