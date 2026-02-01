using System.Security.Claims;
using AccountManagement.Dto.User;
using AccountManagement.Mappers;
using AccountManagement.Services.UserService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountManagement.Controllers;

[ApiController]
[Route("api/v1/[controller]/[action]")]
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

    [Authorize]
    [HttpPut]
    public async Task<IActionResult> Me([FromBody] UserSelfUpdateDto userUpdateDto) {
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