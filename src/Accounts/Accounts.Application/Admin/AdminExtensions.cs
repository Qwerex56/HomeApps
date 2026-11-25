using Accounts.Application.Abstractions;
using Accounts.Application.Common;
using Accounts.Domain.Users;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Accounts.Application.Admin;

internal static class AdminExtensions
{
    /// <summary>Admin or Owner only (checked here, not only by the Api policy); unknown id: 404.</summary>
    public static async Task<Result> ForAdminAsync(
        this IUserRepository users, ICurrentUser currentUser, Guid userId, Func<User, Result> action, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAdmin())
        {
            return Result.Fail(UserErrors.ForbiddenRole);
        }

        var user = await users.GetByIdAsync(userId, cancellationToken);
        return user is null ? Result.Fail(ApplicationErrors.UserNotFound) : action(user);
    }
}
