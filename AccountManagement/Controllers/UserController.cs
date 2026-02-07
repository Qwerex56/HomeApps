using System.Security.Claims;
using AccountManagement.Dto.User;
using AccountManagement.Mappers;
using AccountManagement.Services.UserService;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountManagement.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/[controller]/[action]")]
public class UserController : ControllerBase {
    private readonly IUserService _userService;
    
    private readonly ILogger<UserController> _logger;

    public UserController(IUserService userService, ILogger<UserController> logger) {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet]
    [Route("id/{id:guid}")]
    public async Task<IActionResult> GetUserById(Guid id) {
        var user = await _userService.GetUserByIdAsync(id);

        if (user is null) {
            return NotFound();
        }
        
        return Ok(UserMapper.ToGetUserDto(user));
    }

    [Authorize]
    [HttpGet]
    [ProducesResponseType(typeof(GetUserDto),  StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe() {
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

    [Authorize]
    [HttpPut]
    public async Task<IActionResult> UpdateMe([FromBody] UserSelfUpdateDto userUpdateDto) {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId)) {
            return NotFound();
        }

        await _userService.UpdateUserInfo(userUpdateDto, userId);

        return Ok();
    }

    [Authorize]
    [HttpPut]
    public async Task<IActionResult> DeactivateAccount() {
        var userId = GetUserIdString();

        if (string.IsNullOrEmpty(userId)) {
            return NotFound();
        }
        
        await _userService.UserSoftDeleteById(userId);
        
        Response.Cookies.Delete("refreshToken");
        return Ok();
    }
    
    private string? GetUserIdString() {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}