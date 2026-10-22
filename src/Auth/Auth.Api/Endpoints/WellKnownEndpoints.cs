using Auth.Infrastructure.Security;
using HomeApps.BuildingBlocks.Api.Endpoints;
using Microsoft.Extensions.Options;

namespace Auth.Api.Endpoints;

/// <summary>
/// A-31: what other services (and later MCP clients) need to trust Auth's tokens. Only the issuer and the keys for
/// now; the OAuth endpoints join these documents together with <c>/authorize</c> after 1.0 (auth.md 3.1).
/// </summary>
internal sealed class WellKnownEndpoints : IEndpoint
{
    private const string JwksPath = "/.well-known/jwks.json";

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var wellKnown = app.MapGroup("/.well-known").WithTags("Discovery").AllowAnonymous();

        wellKnown.MapGet("/openid-configuration", (IOptions<SigningOptions> signing) => Results.Ok(Metadata(signing.Value)));

        wellKnown.MapGet("/oauth-authorization-server", (IOptions<SigningOptions> signing) => Results.Ok(Metadata(signing.Value)));

        wellKnown.MapGet("/jwks.json", (SigningKeys keys, HttpContext http) =>
        {
            http.Response.Headers.CacheControl = "public, max-age=300";
            return Results.Ok(new
            {
                keys = keys.Jwks.Keys.Select(k => new { kty = k.Kty, use = k.Use, kid = k.Kid, alg = k.Alg, n = k.N, e = k.E }),
            });
        });
    }

    private static Dictionary<string, object> Metadata(SigningOptions signing)
    {
        var issuer = signing.Issuer.TrimEnd('/');
        return new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["issuer"] = signing.Issuer,
            ["jwks_uri"] = issuer + JwksPath,
            ["subject_types_supported"] = new[] { "public" },
            ["id_token_signing_alg_values_supported"] = new[] { "RS256" },
        };
    }
}
