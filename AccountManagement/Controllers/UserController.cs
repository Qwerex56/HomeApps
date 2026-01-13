using AccountManagement.Dto.User;
using AccountManagement.Mappers;
using AccountManagement.Services.UserService;
using Microsoft.AspNetCore.Mvc;

namespace AccountManagement.Controllers;

[ApiController]
[Route("api/v1/users")]
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
        
        return Ok(user);
    }

    [HttpGet]
    [Route("email/{email}")]
    public async Task<IActionResult> GetUserByEmail(string email) {
        var user = await _userService.GetUserByEmailAsync(email);
        
        if (user is null) {
            return NotFound();
        }
        
        return Ok(UserMapper.ToCreatedUserDto(user));
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto user) {
        var createdUser = await _userService.CreateUserAsync(user);
        
        return CreatedAtAction(
            nameof(GetUserById), 
            new { id = createdUser.Id }, 
            UserMapper.ToCreatedUserDto(createdUser));
    }
}