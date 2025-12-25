using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AuthorizationService.Dtos.Account;
using Microsoft.IdentityModel.Tokens;
using Shared.Authorization.Configuration;

namespace AuthorizationService.Services.JwtService;

/// <summary>
/// Provides functionality for generating JWT access tokens and secure refresh tokens.
/// </summary>
/// <remarks>
/// This service is responsible for creating signed JSON Web Tokens (JWTs) used for
/// authenticating users across the system. It also generates cryptographically secure
/// refresh tokens used for renewing expired access tokens.
/// </remarks>
public class JwtService
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtService"/> class.
    /// </summary>
    /// <param name="configuration">
    /// The application configuration used to load JWT settings such as issuer, audience, and signing key.
    /// </param>
    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Generates a signed JWT access token for the specified user.
    /// </summary>
    /// <param name="user">
    /// The user for whom the token should be generated. The user's ID and email are included as claims.
    /// </param>
    /// <returns>
    /// A string containing the encoded JWT access token.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The generated token includes the following claims:
    /// </para>
    /// <list type="bullet">
    /// <item><description><c>sub</c> — the user's unique identifier (GUID)</description></item>
    /// <item><description><c>email</c> — the user's email address</description></item>
    /// <item><description><c>role</c> — placeholder role value (to be replaced with actual roles)</description></item>
    /// </list>
    ///
    /// <para>
    /// The token is signed using the HMAC SHA-256 algorithm and expires after 1 hour.
    /// </para>
    ///
    /// <para>
    /// JWT configuration values (issuer, audience, signing key) are loaded from the
    /// <see cref="JwtOptions"/> section in the application configuration.
    /// </para>
    /// </remarks>
    public string GenerateToken(UserDto user)
    {
        var jwtOptions = new JwtOptions();
        _configuration.GetSection(JwtOptions.Jwt).Bind(jwtOptions);

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Auth__Jwt__Key"]));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        Claim[] claims =
        [
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, "roles") // TODO: Save user roles
        ];

        var token = new JwtSecurityToken(
            issuer: _configuration["Auth__Jwt__Issuer"],
            audience: _configuration["Auth__Jwt__Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generates a cryptographically secure refresh token.
    /// </summary>
    /// <returns>
    /// A Base64-encoded string representing a securely generated refresh token.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Refresh tokens are generated using <see cref="RandomNumberGenerator"/> to ensure
    /// high entropy and resistance to prediction or brute-force attacks.
    /// </para>
    ///
    /// <para>
    /// The resulting token is 64 bytes long before Base64 encoding.
    /// </para>
    /// </remarks>
    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }
}
