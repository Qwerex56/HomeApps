using System.Security.Claims;
using AccountManagement.Dto.HouseholdDto;
using AccountManagement.Mappers;
using AccountManagement.Services.HouseholdService.HouseholdCommandService;
using AccountManagement.Services.HouseholdService.HouseholdQueryService;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountManagement.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/[controller]/[action]")]
public class HouseholdController : ControllerBase {
    private readonly ILogger<HouseholdController> _logger;

    private readonly IHouseholdQueryService _householdQueryService;
    private readonly IHouseholdCommandService _householdCommandService;

    public HouseholdController(ILogger<HouseholdController> logger, IHouseholdQueryService householdQueryService,
        IHouseholdCommandService householdCommandService) {
        _logger = logger;
        _householdQueryService = householdQueryService;
        _householdCommandService = householdCommandService;
    }

    // --- Queries ---

    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<HouseholdDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUserHouseholds() {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdString)) {
            return Unauthorized();
        }

        var userId = Guid.Parse(userIdString);
        var households = await _householdQueryService.GetUserHouseholdsAsync(userId);

        return Ok(households);
    }

    [HttpGet("{householdId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(HouseholdDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetHousehold(Guid householdId) {
        var household = await _householdQueryService.GetHouseholdAsync(householdId);

        if (household is null) {
            return NotFound();
        }

        return Ok(household);
    }
    
    // --- Commands ---

    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateHousehold([FromBody] CreateHouseholdRequest request) {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId)) {
            return Unauthorized();
        }

        var createHouseholdDto = new CreateHouseholdDto {
            CreatorId = Guid.Parse(userId),
            FamilyName = request.FamilyName
        };
        
        await _householdCommandService.CreateHouseholdAsync(createHouseholdDto);
        return Ok();
    }

    [HttpPut]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateHousehold([FromBody] UpdateHouseholdRequest request) {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId)) {
            return Unauthorized();
        }

        var createDto = HouseholdMapper.ToUpdateDto(request, Guid.Parse(userId));
        
        await _householdCommandService.UpdateHouseholdAsync(createDto);
        return Ok();
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddUser([FromBody] AddUserToHouseholdRequest request) {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId)) {
            return Unauthorized();
        }
        
        var addUserDto = HouseholdMapper.ToAddUserDto(request, Guid.Parse(userId));
        
        await _householdCommandService.AddUserToHouseholdAsync(addUserDto);
        return Ok();
    }

    [HttpDelete]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveUser([FromBody] RemoveUserFromHouseholdRequest request) {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId)) {
            return Unauthorized();
        }
        
        var removeUserDto = HouseholdMapper.ToRemoveUserDto(request, Guid.Parse(userId));
        
        await _householdCommandService.RemoveUserFromHouseholdAsync(removeUserDto);
        return Ok();
    }
}