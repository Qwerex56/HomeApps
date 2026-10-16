using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
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

    public static string CreateToken(Guid userId, string? email = null, params string[] roles)
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

        var token = new JwtSecurityToken(
            Issuer,
            Audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
