using System.Security.Claims;
using AccountManagement.Mappers;
using AccountManagement.Services.HouseholdService.HouseholdCommandService;
using AccountManagement.Services.HouseholdService.HouseholdQueryService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountManagement.Controllers;

[ApiController]
[Route("api/v1/[controller]/[action]")]
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
    public async Task<IActionResult> GetUserHouseholdsAsync() {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdString)) {
            return NotFound();
        }

        var userId = Guid.Parse(userIdString);
        var households = await _householdQueryService.GetUserHouseholdsAsync(userId);

        return Ok(HouseholdMapper.ToHouseholdDtos(households));
    }

    [HttpGet]
    public async Task<IActionResult> GetHousehold([FromBody] Guid householdId) {
        var household = await _householdQueryService.GetHouseholdAsync(householdId);

        if (household is null) {
            return NotFound();
        }

        return Ok(HouseholdMapper.ToHouseholdDto(household));
    }
}