using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;
using Households.Domain.Users;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.AccountsSync;

// Households follows Accounts (S1–S4). Called by the consumers in Infrastructure; the kit's inbox drops duplicates and
// the commands are idempotent anyway. An event for an account not copied yet gives user.pending and is retried (Q5).

/// <summary>S1: <c>UserCreated</c>. A second delivery changes nothing.</summary>
public sealed record CreateUserCopyCommand(Guid UserId, string DisplayName, string? AvatarUrl) : ICommand;

internal sealed class CreateUserCopyHandler(IUserCopies users) : ICommandHandler<CreateUserCopyCommand>
{
    public async Task<Result> Handle(CreateUserCopyCommand command, CancellationToken cancellationToken)
    {
        if (await users.GetAsync(command.UserId, cancellationToken) is null)
        {
            users.Add(UserCopy.Create(command.UserId, command.DisplayName, command.AvatarUrl));
        }

        return Result.Ok();
    }
}

/// <summary>S2: <c>UserProfileUpdated</c>.</summary>
public sealed record UpdateUserCopyCommand(Guid UserId, string DisplayName, string? AvatarUrl) : ICommand;

internal sealed class UpdateUserCopyHandler(IUserCopies users) : ICommandHandler<UpdateUserCopyCommand>
{
    public async Task<Result> Handle(UpdateUserCopyCommand command, CancellationToken cancellationToken)
    {
        var user = await users.GetAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Fail(ApplicationErrors.UserPending);
        }

        user.UpdateProfile(command.DisplayName, command.AvatarUrl);
        return Result.Ok();
    }
}

/// <summary>
/// S3, D6: <c>UserDisabled</c>. Memberships stay, the member shows as inactive. A ban (<paramref name="BySelf"/> false)
/// also hands every household this person owns to the next active Admin or Member (D5, D5b).
/// </summary>
public sealed record DisableUserCopyCommand(Guid UserId, bool BySelf) : ICommand;

internal sealed class DisableUserCopyHandler(IUserCopies users, IHouseholdRepository households, TimeProvider time)
    : ICommandHandler<DisableUserCopyCommand>
{
    public async Task<Result> Handle(DisableUserCopyCommand command, CancellationToken cancellationToken)
    {
        var user = await users.GetAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Fail(ApplicationErrors.UserPending);
        }

        user.Disable();
        if (command.BySelf)
        {
            return Result.Ok();
        }

        var owned = (await households.ListInvolvingAsync(command.UserId, cancellationToken))
            .Where(h => h.Owner?.UserId == command.UserId)
            .ToList();
        var isActive = await users.ActivityOfAsync(owned, cancellationToken);
        foreach (var household in owned)
        {
            household.OwnerBanned(command.UserId, id => id != command.UserId && isActive(id), time);
        }

        return Result.Ok();
    }
}

/// <summary>S3: <c>UserEnabled</c>. A banned Owner stays a Member (D5b).</summary>
public sealed record EnableUserCopyCommand(Guid UserId) : ICommand;

internal sealed class EnableUserCopyHandler(IUserCopies users) : ICommandHandler<EnableUserCopyCommand>
{
    public async Task<Result> Handle(EnableUserCopyCommand command, CancellationToken cancellationToken)
    {
        var user = await users.GetAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Fail(ApplicationErrors.UserPending);
        }

        user.Enable();
        return Result.Ok();
    }
}

/// <summary>
/// S4: <c>UserErased</c> ("forget me"). The person leaves every household (D5 for the ones they owned), disappears as
/// "invited by" and as the author of invites, which keep working (Q6), and the copy is deleted. Unknown user: nothing to do.
/// </summary>
public sealed record ForgetUserCommand(Guid UserId) : ICommand;

internal sealed class ForgetUserHandler(IUserCopies users, IHouseholdRepository households, IInviteRepository invites, TimeProvider time)
    : ICommandHandler<ForgetUserCommand>
{
    public async Task<Result> Handle(ForgetUserCommand command, CancellationToken cancellationToken)
    {
        var involved = await households.ListInvolvingAsync(command.UserId, cancellationToken);
        var isActive = await users.ActivityOfAsync(involved, cancellationToken);
        foreach (var household in involved)
        {
            household.ForgetUser(command.UserId, id => id != command.UserId && isActive(id), time);
        }

        foreach (var invite in await invites.ListByCreatorAsync(command.UserId, cancellationToken))
        {
            invite.ForgetCreator();
        }

        if (await users.GetAsync(command.UserId, cancellationToken) is { } user)
        {
            users.Remove(user);
        }

        return Result.Ok();
    }
}
