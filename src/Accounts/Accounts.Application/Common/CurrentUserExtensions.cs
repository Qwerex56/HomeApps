using Accounts.Domain.Common;
using HomeApps.BuildingBlocks.Application.Abstractions;

namespace Accounts.Application.Common;

internal static class CurrentUserExtensions
{
    /// <summary>The caller's server role from the token's <c>role</c> claim; <see cref="ServerRole.User"/> when missing.</summary>
    public static ServerRole ServerRole(this ICurrentUser user) =>
        user.Roles
            .Select(r => Enum.TryParse<ServerRole>(r, ignoreCase: true, out var role) && Enum.IsDefined(role)
                ? role
                : Domain.Common.ServerRole.User)
            .DefaultIfEmpty(Domain.Common.ServerRole.User)
            .Max();

    /// <summary>The caller as the domain sees them: id and role from the token, never from the body.</summary>
    public static Actor Actor(this ICurrentUser user) => new(user.RequiredUserId, user.ServerRole());

    public static bool IsAdmin(this ICurrentUser user) => user.ServerRole() >= Domain.Common.ServerRole.Admin;
}
