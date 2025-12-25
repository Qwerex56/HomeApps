using AuthorizationService.Dtos.Account;
using Microsoft.AspNetCore.Mvc;

namespace AuthorizationService.Services.AccountService;

/// <summary>
/// Provides an HTTP-based implementation of <see cref="IAccountService"/> for communicating
/// with the AccountManagement microservice.
/// </summary>
/// <remarks>
/// This service is responsible for retrieving user information and persisting refresh tokens
/// by sending HTTP requests to the AccountManagement API. It acts as a client wrapper around
/// <see cref="HttpClient"/>, abstracting away endpoint details from the rest of the application.
/// </remarks>
public class AccountService : IAccountService
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountService"/> class.
    /// </summary>
    /// <param name="httpClient">
    /// The <see cref="HttpClient"/> instance used to send requests to the AccountManagement microservice.
    /// </param>
    public AccountService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Retrieves a user by their email address from the AccountManagement microservice.
    /// </summary>
    /// <param name="email">
    /// The email address of the user to retrieve.
    /// </param>
    /// <returns>
    /// A task that resolves to a <see cref="UserDto"/> when the user exists,
    /// or <c>null</c> when no user is found.
    /// </returns>
    /// <remarks>
    /// In development mode, this method returns a placeholder <see cref="UserDto"/> instead of
    /// performing an HTTP request. This is intended for local testing without requiring the
    /// AccountManagement service to be running.
    /// </remarks>
    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            return new UserDto(Guid.CreateVersion7(), "nil", "nil", "");
        }

        var response = await _httpClient.GetFromJsonAsync<UserDto?>(
            "http://localhost:5000/api/users/" + email); // TODO: change endpoint

        return response;
    }

    /// <summary>
    /// Retrieves a user by their unique identifier (GUID) from the AccountManagement microservice.
    /// </summary>
    /// <param name="userId">
    /// The unique identifier of the user to retrieve.
    /// </param>
    /// <returns>
    /// A task that resolves to a <see cref="UserDto"/> when the user exists,
    /// or <c>null</c> when no user is found.
    /// </returns>
    /// <remarks>
    /// In development mode, this method returns a placeholder <see cref="UserDto"/> instead of
    /// performing an HTTP request. This is intended for local testing without requiring the
    /// AccountManagement service to be running.
    /// </remarks>
    public async Task<UserDto?> GetUserByIdAsync(Guid userId)
    {
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            return new UserDto(Guid.CreateVersion7(), "nil", "nil", "");
        }

        var response = await _httpClient.GetFromJsonAsync<UserDto?>(
            $"http://localhost:5000/api/users/{userId}"); // TODO: change endpoint

        return response;
    }

    /// <summary>
    /// Saves or updates the refresh token associated with a specific user.
    /// </summary>
    /// <param name="userId">
    /// The unique identifier of the user for whom the refresh token should be stored.
    /// </param>
    /// <param name="refreshToken">
    /// The refresh token value to persist.
    /// </param>
    /// <returns>
    /// An <see cref="IActionResult"/> indicating the outcome of the operation.
    /// </returns>
    /// <exception cref="NotImplementedException">
    /// Thrown because this method has not yet been implemented.
    /// </exception>
    /// <remarks>
    /// This method is intended to send a POST or PUT request to the AccountManagement microservice
    /// to persist the refresh token. The implementation is pending and should be completed once
    /// the corresponding API endpoint is available.
    /// </remarks>
    public async Task<IActionResult> SaveRefreshTokenAsync(Guid userId, string refreshToken)
    {
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            return new OkObjectResult("");
        }
        
        throw new NotImplementedException();
    }
}