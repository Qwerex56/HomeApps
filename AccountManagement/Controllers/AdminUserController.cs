using AccountManagement.Dto.User;
using AccountManagement.Services.UserService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Authorization;

namespace AccountManagement.Controllers;

[Authorize(Roles = $"{nameof(UserSystemRoleEnum.SystemAdmin)},{nameof(UserSystemRoleEnum.SystemOwner)}")]
[ApiController]
[Route("api/v1/[controller]")]
public class AdminUserController : ControllerBase {
    private readonly IUserService _userService;

    public AdminUserController(IUserService userService) {
        _userService = userService;
    }

    [HttpGet]
    [Route("id/{id:guid}")]
    public async Task<IActionResult> GetUserById(Guid id) {
        var user = await _userService.GetUserByIdAsync(id);

        if (user is null) {
            return NotFound();
        }
        
        return Ok(user);
    }
    
    [HttpPost]
    [Route("[action]")]
    public async Task<IActionResult> CreateUserAsync([FromBody] CreateUserByAdminDto createUserDto) {
        var createdUser = await _userService.CreateUserWithPasswordAsync(createUserDto);

        return CreatedAtAction(
            nameof(GetUserById),
            new { id = createdUser.Id },
            createdUser
        );
    }
}