using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using HomeApps.BuildingBlocks.Api.Auth;
using Microsoft.IdentityModel.Tokens;

namespace HomeApps.BuildingBlocks.Testing;

/// <summary>
/// Mints access tokens for integration tests. The API under test is reconfigured to trust this key instead of
/// fetching the Auth service's JWKS.
/// </summary>
public static class TestJwt
{
    public const string Issuer = "https://auth.homeapps.test";
    public const string Audience = "homeapps";

    public static SymmetricSecurityKey SigningKey { get; } =
        new(Encoding.UTF8.GetBytes("homeapps-integration-tests-signing-key-not-a-secret"));

    public static string CreateToken(Guid userId, string? email = null, params string[] roles) =>
        Create(userId, email, roles, actor: null);

    /// <summary>
    /// A token like the one Auth delegates to an assistant acting for <paramref name="userId"/>: the person's own
    /// claims plus <c>act</c> = <paramref name="actor"/> and <c>amr</c> = <c>delegated</c> (jarvis-v1.md, JV3).
    /// </summary>
    public static string CreateDelegatedToken(Guid userId, string actor = "jarvis-service", string? email = null, params string[] roles) =>
        Create(userId, email, roles, actor);

    /// <summary>
    /// A token Auth gives a connected app (OAuth, jarvis-v1.md 3.1): <c>aud</c> = the <paramref name="resource"/> it
    /// asked for, <c>client_id</c> = the app.
    /// </summary>
    public static string CreateAppToken(Guid userId, string resource, string clientId) =>
        Create(userId, null, [], null, resource, clientId);

    private static string Create(Guid userId, string? email, string[] roles, string? actor, string audience = Audience, string? clientId = null)
    {
        ArgumentNullException.ThrowIfNull(roles);

        var claims = new List<Claim>
        {
            new(ClaimNames.Subject, userId.ToString()),
            new(ClaimNames.TokenId, Guid.NewGuid().ToString()),
        };
        if (email is not null)
        {
            claims.Add(new Claim(ClaimNames.Email, email));
        }

        claims.AddRange(roles.Select(r => new Claim(ClaimNames.Role, r)));
        if (clientId is not null)
        {
            claims.Add(new Claim(ClaimNames.ClientId, clientId));
        }

        if (actor is not null)
        {
            claims.Add(new Claim(ClaimNames.Actor, JsonSerializer.Serialize(new Dictionary<string, string> { ["sub"] = actor }), JsonClaimValueTypes.Json));
            claims.Add(new Claim(ClaimNames.AuthenticationMethods, "delegated"));
        }

        var token = new JwtSecurityToken(
            Issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
