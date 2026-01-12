using AccountManagement.Dto.User;
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
    [Route("{id:guid}")]
    public async Task<IActionResult> GetUserById(Guid id) {
        var user = await _userService.GetUserByIdAsync(id);

        if (user is null) {
            return NotFound();
        }
        
        return Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto user) {
        var result = await _userService.CreateUserAsync(user);
        
        return CreatedAtAction(nameof(GetUserById), new { id = result.Id }, result);
    }
}