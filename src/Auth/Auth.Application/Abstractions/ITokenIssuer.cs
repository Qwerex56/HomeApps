using Auth.Domain.Identities;

namespace Auth.Application.Abstractions;

/// <summary>Signs access tokens and short-lived tickets (RS256, keys published as JWKS, A-31).</summary>
public interface ITokenIssuer
{
    /// <summary>
    /// A 15-minute JWT with <c>sub</c>, <c>email</c>, <c>role</c>, <c>client_id</c> and <c>auth_time</c>
    /// (<see cref="IssuedRefreshToken.AuthenticatedAt"/>), for the client's audiences.
    /// </summary>
    AccessToken IssueAccessToken(Identity identity, IssuedRefreshToken session, RegisteredClient client);

    /// <summary>A 10-minute signed ticket with its own <c>scope</c>; never accepted as an access token.</summary>
    string IssueTicket(Ticket ticket);

    /// <summary>The ticket, or null when it is forged, expired or not a ticket at all.</summary>
    Ticket? ReadTicket(string value);
}

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);

/// <summary>What a ticket lets its holder do next. The holder is not signed in yet.</summary>
public abstract record Ticket
{
    private protected Ticket() { }
}

/// <summary>
/// A-09: the user signed in correctly to an account they disabled themselves. Accounts accepts it too
/// (<c>GET /users/me/reactivation</c>), then Auth exchanges it for tokens.
/// </summary>
public sealed record ReactivationTicket(Guid IdentityId, AuthMethod Method) : Ticket;

/// <summary>A-15: Google/Apple returned an email that already has an account; linking waits for the user's consent.</summary>
public sealed record ExternalLinkTicket(Guid IdentityId, ExternalProvider Provider, string Subject, string Email) : Ticket;
