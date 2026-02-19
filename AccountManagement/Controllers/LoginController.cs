using AccountManagement.Dto.Credentials;
using AccountManagement.Dto.LoginDto;
using AccountManagement.Services.CookieService;
using AccountManagement.Services.LoginService;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AccountManagement.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/[controller]/[action]")]
public class LoginController : ControllerBase {
    private readonly ILoginService _loginService;
    private readonly ICookieService _cookieService;

    public LoginController(ILoginService loginService, ICookieService cookieService) {
        _loginService = loginService;
        _cookieService = cookieService;
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

        _cookieService.SetRefreshToken(Response, refreshToken.Token, refreshToken.Expires);

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

        _cookieService.RemoveRefreshToken(Response);

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

        _cookieService.SetRefreshToken(Response, tokens.RefreshToken.Token, tokens.RefreshToken.Expires);

        return Ok(tokens.JwtToken);
    }
}