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

    IReadOnlyCollection<string> Roles { get; }

    bool IsInRole(string role);

    /// <summary>
    /// The user id for code that only runs behind authentication. Throws if there is no authenticated user,
    /// which means an endpoint was opened to anonymous callers by mistake.
    /// </summary>
    Guid RequiredUserId => UserId ?? throw new InvalidOperationException("There is no authenticated user on this request.");
}
