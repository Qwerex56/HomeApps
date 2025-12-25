using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthorizationService.Services.AccountService;
using AuthorizationService.Services.JwtService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthorizationService.Controllers;

/// <summary>
/// Provides endpoints for generating JWT access tokens and refresh tokens
/// for authenticated users.
/// </summary>
/// <remarks>
/// This controller requires authentication and is responsible for issuing
/// short-lived JWT access tokens and long-lived refresh tokens. Tokens are
/// returned to the client via secure HttpOnly cookies.
/// </remarks>
[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class JwtController : ControllerBase
{
    private readonly JwtService _jwtService;
    private readonly IAccountService _accountService;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtController"/> class.
    /// </summary>
    /// <param name="jwtService">Service responsible for generating JWT and refresh tokens.</param>
    /// <param name="accountService">Service used to retrieve user information from AccountManagement.</param>
    public JwtController(JwtService jwtService, IAccountService accountService)
    {
        _jwtService = jwtService;
        _accountService = accountService;
    }

    /// <summary>
    /// Generates a new JWT access token and refresh token for the authenticated user.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This endpoint requires the user to already be authenticated (e.g., via Google OAuth).
    /// The user's email is extracted from the authentication principal and used to retrieve
    /// the corresponding user record from the AccountManagement microservice.
    /// </para>
    ///
    /// <para>
    /// If the user exists, a new JWT access token and refresh token are generated and stored
    /// in secure HttpOnly cookies:
    /// </para>
    /// <list type="bullet">
    /// <item><description><c>jwt</c> — expires in 15 minutes</description></item>
    /// <item><description><c>refreshToken</c> — expires in 30 days</description></item>
    /// </list>
    ///
    /// <para>
    /// The endpoint returns <c>200 OK</c> with no body on success.
    /// </para>
    /// </remarks>
    /// <returns>
    /// <para>
    /// <see cref="IActionResult"/> representing one of the following outcomes:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description><c>200 OK</c> — tokens were successfully generated and stored in cookies.</description>
    /// </item>
    /// <item>
    /// <description><c>403 Forbid</c> — the authenticated principal does not contain an email claim or the user does not exist.</description>
    /// </item>
    /// </list>
    /// </returns>
    /// <response code="200">Tokens were successfully generated and stored in cookies.</response>
    /// <response code="403">The user is authenticated but cannot be resolved or lacks required claims.</response>
    [Authorize]
    [HttpGet]
    [Route("token")]
    public async Task<IActionResult> Token()
    {
        var email = HttpContext.User.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrEmpty(email))
        {
            return Forbid();
        }

        var user = await _accountService.GetUserByEmailAsync(email);

        if (user is null)
        {
            return Forbid();
        }

        var token = _jwtService.GenerateToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        Response.Cookies.Append("jwt", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddMinutes(15)
        });

        Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(30)
        });

        return Ok();
    }

    [HttpGet]
    [Route("refreshToken")]
    public async Task<IActionResult> RefreshToken()
    {
        var oldJwt = Request.Cookies["jwt"];
        var oldRefresh = Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(oldJwt) || string.IsNullOrEmpty(oldRefresh))
        {
            return Unauthorized();
        }

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(oldJwt);
        var userId = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _accountService.GetUserByIdAsync(Guid.Parse(userId));

        if (user is null || user.RefreshToken != oldRefresh)
        {
            return Unauthorized();
        }

        var newJwt = _jwtService.GenerateToken(user);
        var newRefresh = _jwtService.GenerateRefreshToken();

        await _accountService.SaveRefreshTokenAsync(Guid.Parse(userId), newRefresh);

        Response.Cookies.Append("jwt", newJwt,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(15)
            });
        Response.Cookies.Append("refreshToken", newRefresh,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(30)
            });

        return Ok();
    }
}