using System.Security.Claims;
using System.Text.Json;
using AuthorizationService.Dtos.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthorizationService.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class LoginController : ControllerBase
{
    private HttpClient _httpClient;

    public LoginController(IHttpClientFactory clientFactory)
    {
        _httpClient = clientFactory.CreateClient();
    }

    [HttpGet]
    [Route("")]
    [Route("login")]
    public async Task Login()
    {
        await HttpContext.ChallengeAsync(GoogleDefaults.AuthenticationScheme,
            new AuthenticationProperties { RedirectUri = "/api/v1/Login/post-login" });
    }

    [HttpGet]
    [Route("post-login")]
    public async Task<IActionResult> PostLogin()
    {
        var email = HttpContext.User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email))
        {
            // user not logged-in redirect to Google OAuth
            return Redirect("api/v1/Login");
        }

        // api/v1/accounts/users?email={email}
        var responseMessage = await _httpClient.GetAsync($"http://localhost:5130/WeatherForecast");
        if (!responseMessage.IsSuccessStatusCode)
        {
            // user doesn't exists in DB - redirect to create account site
            return BadRequest(responseMessage.Content.ReadAsStringAsync().Result);
        }

        UserDto? user = null; //= await responseMessage.Content.ReadFromJsonAsync<UserDto>();

        try
        {
            user = await responseMessage.Content.ReadFromJsonAsync<UserDto>();
        }
        catch (JsonException ex)
        {
            Console.WriteLine("Deserialization failed: " + ex.Message);
            // return BadRequest("Invalid JSON for UserDto");
        }

        if (user is null)
        {
            return Ok(); // TODO: redirect to account-create site
        }

        return Redirect("api/v1/Login/token");
    }

    [Authorize]
    [HttpGet]
    [Route("me")]
    public async Task<ActionResult<UserDto>> Me()
    {
        var email = HttpContext.User.FindFirst(ClaimTypes.Email)?.Value;
        var responseMessage = await _httpClient.GetAsync($"api/v1/accounts/users?email={email}");

        throw new NotImplementedException();
    }

    [HttpGet]
    [Route("token")]
    public async Task<JsonResult> Token()
    {
        throw new NotImplementedException();
    }
}