using AuthorizationService.Dtos.Account;
using Microsoft.AspNetCore.Mvc;

namespace AuthorizationService.Services.AccountService;

/// <summary>
/// Provides access to user-related operations in the AccountManagement microservice.
/// </summary>
/// <remarks>
/// This service acts as a communication layer between the AuthorizationService and
/// the AccountManagement microservice. It is responsible for retrieving user data
/// and persisting refresh tokens associated with authenticated users.
/// </remarks>
public interface IAccountService
{
    /// <summary>
    /// Retrieves a user by their email address.
    /// </summary>
    /// <param name="email">
    /// The email address of the user to retrieve.
    /// </param>
    /// <returns>
    /// A task that resolves to a <see cref="UserDto"/> when the user exists,
    /// or <c>null</c> when no user is found.
    /// </returns>
    /// <remarks>
    /// This method is typically used during the Google OAuth login flow to determine
    /// whether the authenticated Google account corresponds to an existing user.
    /// </remarks>
    Task<UserDto?> GetUserByEmailAsync(string email);

    /// <summary>
    /// Retrieves a user by their unique identifier (GUID).
    /// </summary>
    /// <param name="userId">
    /// The unique identifier of the user to retrieve.
    /// </param>
    /// <returns>
    /// A task that resolves to a <see cref="UserDto"/> when the user exists,
    /// or <c>null</c> when no user is found.
    /// </returns>
    /// <remarks>
    /// This method is used when resolving the authenticated user from the JWT <c>sub</c> claim.
    /// </remarks>
    Task<UserDto?> GetUserByIdAsync(Guid userId);

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
    /// Typically returns <c>204 No Content</c> on success.
    /// </returns>
    /// <remarks>
    /// This method is invoked during the login and token refresh processes to ensure
    /// that the user's refresh token is securely stored in the AccountManagement microservice.
    /// </remarks>
    Task<IActionResult> SaveRefreshTokenAsync(Guid userId, string refreshToken);
}