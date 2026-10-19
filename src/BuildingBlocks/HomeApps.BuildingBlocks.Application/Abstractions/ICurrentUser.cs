namespace HomeApps.BuildingBlocks.Application.Abstractions;

/// <summary>
/// The caller of the current request, taken from the access token only. Never take "who am I" from a URL or body.
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    /// <summary>The <c>sub</c> claim, or <c>null</c> for anonymous callers.</summary>
    Guid? UserId { get; }

    string? Email { get; }

    /// <summary>The <c>auth_time</c> claim: when the user last proved who they are, not when the token was refreshed.</summary>
    DateTimeOffset? AuthenticatedAt { get; }

    IReadOnlyCollection<string> Roles { get; }

    bool IsInRole(string role);

    /// <summary>
    /// The user id for code that only runs behind authentication. Throws if there is no authenticated user,
    /// which means an endpoint was opened to anonymous callers by mistake.
    /// </summary>
    Guid RequiredUserId { get; }
}
