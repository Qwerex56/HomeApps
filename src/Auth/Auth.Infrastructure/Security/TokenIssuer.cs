using System.Globalization;
using Auth.Application.Abstractions;
using Auth.Domain.Identities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Infrastructure.Security;

/// <summary>
/// RS256 JWTs. Access tokens (RFC 9068 <c>at+jwt</c>) are what every service accepts. Tickets use their own type and
/// the <see cref="TicketAudience"/>, so no service ever takes a ticket for an access token, and vice versa.
/// </summary>
internal sealed class TokenIssuer(SigningKeys keys, IOptions<SigningOptions> options, TimeProvider time) : ITokenIssuer
{
    public static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan TicketLifetime = TimeSpan.FromMinutes(10);

    public const string TicketAudience = "homeapps-ticket";
    private static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(30);

    private const string AccessTokenType = "at+jwt";
    private const string TicketType = "ticket+jwt";

    private const string Scope = "scope";
    private const string ReactivationScope = "reactivation";
    private const string ExternalLinkScope = "external_link";

    private static readonly JsonWebTokenHandler Handler = new() { SetDefaultTimesOnTokenCreation = false };

    public AccessToken IssueAccessToken(Identity identity, IssuedRefreshToken session, RegisteredClient client)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(client);

        var now = time.GetUtcNow();
        var expires = now + AccessTokenLifetime;
        var descriptor = Descriptor(AccessTokenType, client.Audiences, now, expires, new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = identity.Id.ToString(),
            [JwtRegisteredClaimNames.Email] = identity.Email.Value,
            ["role"] = identity.Role.ToString(),
            ["client_id"] = client.ClientId,
            [JwtRegisteredClaimNames.AuthTime] = session.AuthenticatedAt.ToUnixTimeSeconds(),
            [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
        });
        return new AccessToken(Handler.CreateToken(descriptor), expires);
    }

    public string IssueTicket(Ticket ticket)
    {
        var claims = ticket switch
        {
            ReactivationTicket r => new Dictionary<string, object>
            {
                [Scope] = ReactivationScope,
                [JwtRegisteredClaimNames.Sub] = r.IdentityId.ToString(),
                ["method"] = r.Method.ToString(),
            },
            ExternalLinkTicket l => new Dictionary<string, object>
            {
                [Scope] = ExternalLinkScope,
                [JwtRegisteredClaimNames.Sub] = l.IdentityId.ToString(),
                ["provider"] = l.Provider.ToString(),
                ["provider_sub"] = l.Subject,
                [JwtRegisteredClaimNames.Email] = l.Email,
            },
            _ => throw new ArgumentException($"Unknown ticket {ticket?.GetType().Name}.", nameof(ticket)),
        };
        claims[JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString();

        var now = time.GetUtcNow();
        return Handler.CreateToken(Descriptor(TicketType, [TicketAudience], now, now + TicketLifetime, claims));
    }

    public async Task<Ticket?> ReadTicketAsync(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var result = await Handler.ValidateTokenAsync(value, new TokenValidationParameters
        {
            ValidIssuer = options.Value.Issuer,
            ValidAudience = TicketAudience,
            ValidTypes = [TicketType],
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            IssuerSigningKeys = keys.ValidationKeys,
            LifetimeValidator = (notBefore, expires, _, _) =>
            {
                var now = time.GetUtcNow().UtcDateTime;
                return notBefore <= now + ClockSkew && expires > now - ClockSkew;
            },
        });
        var token = result.SecurityToken as JsonWebToken;
        if (!result.IsValid || token is null)
        {
            return null;
        }

        string Claim(string name) => token.TryGetPayloadValue<string>(name, out var v) ? v : string.Empty;
        if (!Guid.TryParse(Claim(JwtRegisteredClaimNames.Sub), CultureInfo.InvariantCulture, out var identityId))
        {
            return null;
        }

        return Claim(Scope) switch
        {
            ReactivationScope when Enum.TryParse<AuthMethod>(Claim("method"), out var method) =>
                new ReactivationTicket(identityId, method),
            ExternalLinkScope when Enum.TryParse<ExternalProvider>(Claim("provider"), out var provider) =>
                new ExternalLinkTicket(identityId, provider, Claim("provider_sub"), Claim(JwtRegisteredClaimNames.Email)),
            _ => null,
        };
    }

    private SecurityTokenDescriptor Descriptor(
        string type, IEnumerable<string> audiences, DateTimeOffset now, DateTimeOffset expires, Dictionary<string, object> claims)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            TokenType = type,
            Issuer = options.Value.Issuer,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = keys.SigningCredentials,
            Claims = claims,
        };
        foreach (var audience in audiences)
        {
            descriptor.Audiences.Add(audience);
        }

        return descriptor;
    }
}
