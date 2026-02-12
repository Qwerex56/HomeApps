using AccountManagement.Dto.Credentials;
using AccountManagement.Dto.LoginDto;
using AccountManagement.Services.LoginService;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AccountManagement.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/[controller]/[action]")]
public class LoginController : ControllerBase {
    private readonly ILoginService _loginService;

    public LoginController(ILoginService loginService) {
        _loginService = loginService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserLoggedInDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Login([FromBody] UserCredentialsDto credentials) {
        var user = await _loginService.ValidateCredentials(credentials);

        if (user is null) {
            return Unauthorized();
        }

        var jwt = await _loginService.GenerateJwtTokenAsync(user.Id);
        var refreshToken = await _loginService.GenerateRefreshTokenAsync(user.Id);

        var loggedUserDto = new UserLoggedInDto {
            UserId = user.Id,
            DisplayName = user.Name,
            EmailAddress = user.UserCredential.Email,

            Token = jwt
        };

        Response.Cookies.Append("refreshToken", refreshToken.Token, new CookieOptions {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = refreshToken.Expires,
            Path = "/",
            Domain = "app.localhost"
        });

        return Ok(loggedUserDto);
    }
    
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Logout() {
        var rawToken = Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(rawToken)) {
            return Ok();
        }
        
        await _loginService.RemoveRefreshTokenWithHash(rawToken);
        
        Response.Cookies.Delete("refreshToken", new CookieOptions {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/",
            Domain = "app.localhost"
        });
        
        return Ok();
    }
    
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken() {
        var rawToken = Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(rawToken)) {
            return Unauthorized();
        }
        
        var tokens = await _loginService.RefreshUserSession(rawToken);
        
        Response.Cookies.Append("refreshToken", tokens.RefreshToken.Token, new CookieOptions {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires =  tokens.RefreshToken.Expires,
            Path = "/",
            Domain = "app.localhost"
        });

        return Ok(tokens.JwtToken);
    }
}