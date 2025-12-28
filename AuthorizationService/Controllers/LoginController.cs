using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthorizationService.Services.AccountService;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthorizationService.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class LoginController : ControllerBase
{
    private readonly IAccountService _accountService;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginController"/> class.
    /// </summary>
    /// <param name="accountService">
    /// Service used to retrieve user information from the AccountManagement microservice.
    /// </param>
    public LoginController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    /// <summary>
    /// Initiates the Google OAuth 2.0 login flow.
    /// </summary>
    /// <remarks>
    /// This endpoint triggers an authentication challenge using the Google authentication scheme.
    /// The user is redirected to Google's login page. After successful authentication,
    /// Google redirects back to the <c>/api/v1/Login/post-login</c> endpoint.
    /// </remarks>
    /// <returns>
    /// A task that, when completed, issues an authentication challenge and results in an HTTP redirect.
    /// No JSON response body is returned.
    /// </returns>
    /// <response code="302">Redirects the user to the Google OAuth login page.</response>
    [HttpGet]
    [Route("")]
    [Route("login")]
    public async Task Login()
    {
        await HttpContext.ChallengeAsync(
            GoogleDefaults.AuthenticationScheme,
            new AuthenticationProperties { RedirectUri = "/api/v1/Login/post-login" });
    }

    /// <summary>
    /// Handles the callback after Google OAuth authentication and resolves the user profile.
    /// </summary>
    /// <remarks>
    /// This endpoint is invoked after a successful Google OAuth login.
    /// It extracts the email claim from the authenticated Google identity and queries
    /// the AccountManagement microservice for a matching user. If the user exists, the
    /// corresponding user DTO is returned. If the email is missing, the client is redirected
    /// to the initial login endpoint.
    /// </remarks>
    /// <returns>
    /// <para>
    /// <see cref="IActionResult"/> that represents one of the following:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// <see cref="OkObjectResult"/> (200) containing the user DTO when the user exists.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <see cref="NotFoundObjectResult"/> (404) containing the email address when no user is found.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <see cref="RedirectResult"/> (302) redirecting to <c>/api/v1/Login</c> when the user is not authenticated with Google.
    /// </description>
    /// </item>
    /// </list>
    /// </returns>
    /// <response code="200">User exists and the user DTO is returned.</response>
    /// <response code="302">User is not authenticated with Google and is redirected to the login endpoint.</response>
    /// <response code="404">No user was found in AccountManagement for the given email.</response>
    [HttpGet]
    [Route("post-login")]
    public async Task<IActionResult> PostLogin()
    {
        var email = HttpContext.User.FindFirst(ClaimTypes.Email)?.Value;
        var test = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(email))
        {
            // user not logged-in redirect to Google OAuth
            return Redirect("https://localhost:7252/api/v1/Login");
        }

        var user = await _accountService.GetUserByEmailAsync(email);
        if (user is null)
        {
            return NotFound(email);
        }

        return Ok(user);
    }

    [Authorize]
    [HttpGet]
    [Route("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            await _accountService.SaveRefreshTokenAsync(Guid.Parse(userId), "");
        }

        Response.Cookies.Append("jwt", "", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            Expires = DateTime.UnixEpoch,
            SameSite = SameSiteMode.Strict
        });

        Response.Cookies.Append("refreshToken", "", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            Expires = DateTime.UnixEpoch,
            SameSite = SameSiteMode.Strict
        });

        return NoContent();
    }

    /// <summary>
    /// Returns the currently authenticated user's profile based on the GUID stored in the JWT.
    /// </summary>
    /// <remarks>
    /// This endpoint reads the <c>sub</c> claim from the validated JWT access token,
    /// interprets it as the user identifier (GUID), and retrieves the corresponding user
    /// from the AccountManagement microservice. The endpoint is protected and requires
    /// a valid JWT, typically provided via an HttpOnly cookie.
    /// </remarks>
    /// <returns>
    /// <para>
    /// <see cref="IActionResult"/> that represents one of the following:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// <see cref="OkObjectResult"/> (200) containing the user DTO when the user exists.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <see cref="NotFoundObjectResult"/> (404) when the <c>sub</c> claim is missing
    /// or the user cannot be found by the provided GUID.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <see cref="UnauthorizedResult"/> (401) may be returned by the authentication middleware
    /// if the JWT is missing or invalid, before this action is executed.
    /// </description>
    /// </item>
    /// </list>
    /// </returns>
    /// <response code="200">User exists and the user DTO is returned.</response>
    /// <response code="401">The request is not authenticated (missing or invalid JWT).</response>
    /// <response code="404">The user could not be resolved from the JWT or no user exists for the given ID.</response>
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet]
    [Route("me")]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return NotFound(new { message = "User not found", user = userId }); // TODO: Turn into object
        }

        var user = await _accountService.GetUserByIdAsync(Guid.Parse(userId));

        if (user is null)
        {
            return NotFound(new { message = "User not found", user = userId });
        }

        return Ok(user);
    }
}