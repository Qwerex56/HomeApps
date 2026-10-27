using Accounts.Application.Abstractions;
using Accounts.Application.Common;
using Accounts.Domain.Users;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Accounts.Application.Admin;

/// <summary>UC-13: a higher role replaces someone's profile (R1 in the domain).</summary>
public sealed record UpdateUserProfileCommand(Guid UserId, ProfileInput Profile) : ICommand;

internal sealed class UpdateUserProfileValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileValidator() => RuleFor(c => c.Profile).NotNull().SetValidator(new ProfileInputValidator());
}

internal sealed class UpdateUserProfileHandler(IUserRepository users, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<UpdateUserProfileCommand>
{
    public async Task<Result> Handle(UpdateUserProfileCommand command, CancellationToken cancellationToken)
    {
        var profile = command.Profile.ToProfile(time);
        if (profile.IsFailed)
        {
            return profile.ToResult();
        }

        return await users.ForAdminAsync(currentUser, command.UserId, u => u.UpdateProfile(currentUser.Actor(), profile.Value, time), cancellationToken);
    }
}

/// <summary>UC-14: disable an account with an optional reason (R1, R3).</summary>
public sealed record DisableUserCommand(Guid UserId, string? Reason) : ICommand;

internal sealed class DisableUserHandler(IUserRepository users, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<DisableUserCommand>
{
    public Task<Result> Handle(DisableUserCommand command, CancellationToken cancellationToken) =>
        users.ForAdminAsync(currentUser, command.UserId, u => u.Disable(currentUser.Actor(), command.Reason, time), cancellationToken);
}

/// <summary>UC-14: enable any disabled account below the caller's role (R1).</summary>
public sealed record EnableUserCommand(Guid UserId) : ICommand;

internal sealed class EnableUserHandler(IUserRepository users, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<EnableUserCommand>
{
    public Task<Result> Handle(EnableUserCommand command, CancellationToken cancellationToken) =>
        users.ForAdminAsync(currentUser, command.UserId, u => u.Enable(currentUser.Actor(), time), cancellationToken);
}

/// <summary>
/// UC-17, R5: the Owner erases a disabled account for a real "forget me" request, confirmed by its email (Q1).
/// The user and its history go; an anonymous trace stays. <c>UserErased</c> reaches Auth and Household.
/// </summary>
public sealed record EraseUserCommand(Guid UserId, string ConfirmEmail) : ICommand;

internal sealed class EraseUserValidator : AbstractValidator<EraseUserCommand>
{
    public EraseUserValidator() => RuleFor(c => c.ConfirmEmail).NotEmpty();
}

internal sealed class EraseUserHandler(IUserRepository users, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<EraseUserCommand>
{
    public Task<Result> Handle(EraseUserCommand command, CancellationToken cancellationToken) =>
        users.ForAdminAsync(currentUser, command.UserId, user =>
        {
            var erased = user.Erase(currentUser.Actor(), command.ConfirmEmail, time);
            if (erased.IsFailed)
            {
                return erased.ToResult();
            }

            users.Remove(user);
            users.AddErased(erased.Value);
            return Result.Ok();
        }, cancellationToken);
}

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
