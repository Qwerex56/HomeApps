using System.Security.Claims;
using HomeApps.BuildingBlocks.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace HomeApps.BuildingBlocks.Api.Auth;

/// <summary><see cref="ICurrentUser"/> read from the validated access token of the current HTTP request.</summary>
internal sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid? UserId =>
        IsAuthenticated && Guid.TryParse(Principal!.FindFirstValue(ClaimNames.Subject), out var id) ? id : null;

    public string? Email => IsAuthenticated ? Principal!.FindFirstValue(ClaimNames.Email) : null;

    public IReadOnlyCollection<string> Roles =>
        IsAuthenticated ? Principal!.FindAll(ClaimNames.Role).Select(c => c.Value).ToArray() : [];

    public bool IsInRole(string role) => IsAuthenticated && Principal!.IsInRole(role);
}
