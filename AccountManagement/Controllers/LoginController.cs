using System.Security.Claims;
using AccountManagement.Dto.Credentials;
using AccountManagement.Dto.LoginDto;
using AccountManagement.Mappers;
using AccountManagement.Services.LoginService;
using AccountManagement.Services.UserService;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountManagement.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/[controller]/[action]")]
public class LoginController : ControllerBase {
    private readonly IUserService _userService;
    private readonly ILoginService _loginService;

    public LoginController(IUserService userService, ILoginService loginService) {
        _userService = userService;
        _loginService = loginService;
    }

    [HttpPost]
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

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Me() {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId)) {
            return NotFound();
        }

        var user = await _userService.GetUserByIdAsync(Guid.Parse(userId));

        if (user is null) {
            return NotFound();
        }

        return Ok(UserMapper.ToGetUserDto(user));
    }
}