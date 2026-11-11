using Auth.Api.ConnectedApps;
using Auth.Infrastructure.Security;
using HomeApps.BuildingBlocks.Api.Endpoints;
using Microsoft.Extensions.Options;

namespace Auth.Api.Endpoints;

/// <summary>
/// A-31: what other services and OAuth clients need: the issuer, the keys, and the OAuth endpoints for connected apps
/// (RFC 8414, jarvis-v1.md 3.1). OAuth addresses are built from the public issuer: browsers and apps come from outside.
/// <para>
/// Accounts plan Q10 (self-hosting): <c>jwks_uri</c> points at the address the caller used, while <c>issuer</c> stays
/// the public one. Services read the keys inside the Docker network (<c>http://auth:8080</c>) without certificates
/// or a round trip through the public domain; browsers behind Traefik still get <c>https://{public host}</c>.
/// </para>
/// </summary>
internal sealed class WellKnownEndpoints : IEndpoint
{
    private const string JwksPath = "/.well-known/jwks.json";

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var wellKnown = app.MapGroup("/.well-known").WithTags("Discovery").AllowAnonymous();

        wellKnown.MapGet("/openid-configuration", (IOptions<SigningOptions> signing, HttpRequest request) =>
            Results.Ok(Metadata(signing.Value, request)))
            .RequireCors(OAuthCors.Policy);

        wellKnown.MapGet("/oauth-authorization-server", (IOptions<SigningOptions> signing, HttpRequest request) =>
            Results.Ok(Metadata(signing.Value, request)))
            .RequireCors(OAuthCors.Policy);

        wellKnown.MapGet("/jwks.json", (SigningKeys keys, HttpContext http) =>
        {
            http.Response.Headers.CacheControl = "public, max-age=300";
            return Results.Ok(new
            {
                keys = keys.Jwks.Keys.Select(k => new { kty = k.Kty, use = k.Use, kid = k.Kid, alg = k.Alg, n = k.N, e = k.E }),
            });
        });
    }

    private static Dictionary<string, object> Metadata(SigningOptions signing, HttpRequest request)
    {
        // Scheme comes from X-Forwarded-Proto behind Traefik (UseForwardedHeaders), the host is passed through as is.
        var caller = $"{request.Scheme}://{request.Host}{request.PathBase}";
        var issuer = signing.Issuer.TrimEnd('/');
        return new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["issuer"] = signing.Issuer,
            ["jwks_uri"] = caller + JwksPath,
            ["subject_types_supported"] = new[] { "public" },
            ["id_token_signing_alg_values_supported"] = new[] { "RS256" },
            ["authorization_endpoint"] = issuer + "/auth/oauth/authorize",
            ["token_endpoint"] = issuer + "/auth/oauth/token",
            ["registration_endpoint"] = issuer + "/auth/oauth/register",
            ["revocation_endpoint"] = issuer + "/auth/oauth/revoke",
            ["response_types_supported"] = new[] { "code" },
            ["grant_types_supported"] = new[] { "authorization_code", "refresh_token" },
            ["code_challenge_methods_supported"] = new[] { "S256" },
            ["token_endpoint_auth_methods_supported"] = new[] { "none" },
            ["revocation_endpoint_auth_methods_supported"] = new[] { "none" },
            ["scopes_supported"] = new[] { "homeapps" },
        };
    }
}
