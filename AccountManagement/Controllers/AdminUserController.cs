using AccountManagement.Dto.User;
using AccountManagement.Models;
using AccountManagement.Services.UserService;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Authorization;

namespace AccountManagement.Controllers;

[Authorize(Roles = $"{nameof(UserSystemRoleEnum.SystemAdmin)},{nameof(UserSystemRoleEnum.SystemOwner)}")]
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/[controller]")]
public class AdminUserController : ControllerBase {
    private readonly IUserService _userService;

    public AdminUserController(IUserService userService) {
        _userService = userService;
    }

    [HttpGet]
    [Route("id/{id:guid}")]
    [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUserById(Guid id) {
        var user = await _userService.GetUserByIdAsync(id);

        if (user is null) {
            return NotFound();
        }
        
        return Ok(user);
    }
    
    [HttpPost]
    [Route("[action]")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateUserAsync([FromBody] CreateUserByAdminDto createUserDto) {
        var createdUser = await _userService.CreateUserWithPasswordAsync(createUserDto);
        
        return CreatedAtAction(
            nameof(GetUserById),
            new { id = createdUser.Id },
            createdUser
        );
    }
}