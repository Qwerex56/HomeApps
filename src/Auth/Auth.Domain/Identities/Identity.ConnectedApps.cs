using FluentResults;

namespace Auth.Domain.Identities;

/// <summary>Apps the person connected through OAuth (jarvis-v1.md 3.1): each one is a session of its own.</summary>
public sealed partial class Identity
{
    /// <summary>
    /// The person allowed <paramref name="client"/> to act for them on <paramref name="audience"/>, and the app has just
    /// exchanged its authorization code. Counts as activity (Accounts' LastSeenAt), not as a new sign-in method.
    /// </summary>
    public Result<IssuedRefreshToken> ConnectApp(ClientInfo client, string audience, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(audience);
        ArgumentNullException.ThrowIfNull(time);

        var now = time.GetUtcNow();
        if (!CanSignIn(now))
        {
            return Result.Fail<IssuedRefreshToken>(IdentityErrors.CannotSignIn);
        }

        var (session, token) = Session.Start(Id, client, AuthMethod.ConnectedApp, now, audience);
        _sessions.Add(session);
        RaiseSignedIn(null, client, now);
        Touch(now);
        return token;
    }

    /// <summary>"Connected apps" in the account settings: connected apps that can still refresh, most recently used first.</summary>
    public IReadOnlyList<Session> ConnectedApps(DateTimeOffset now) =>
        [.. ActiveSessions(now).Where(s => s.IsConnectedApp)];

    /// <summary>"My sessions": HomeApps' own clients only, the connected apps have their own list.</summary>
    public IReadOnlyList<Session> DeviceSessions(DateTimeOffset now) =>
        [.. ActiveSessions(now).Where(s => !s.IsConnectedApp)];
}
