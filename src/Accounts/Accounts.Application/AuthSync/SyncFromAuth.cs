using Accounts.Application.Abstractions;
using Accounts.Application.Common;
using Accounts.Domain.Common;
using Accounts.Domain.Users;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using Contracts = HomeApps.Contracts.Auth;

namespace Accounts.Application.AuthSync;

/// <summary>
/// UC-07: <c>IdentityEmailChanged</c>. Like the other sync commands: a user not created yet (events out of order)
/// gives <c>user.pending</c>, so the consumer throws and the message is retried; an erased user is ignored.
/// </summary>
public sealed record SyncEmailCommand(Guid UserId, string Email) : ICommand;

internal sealed class SyncEmailHandler(IUserRepository users, TimeProvider time) : ICommandHandler<SyncEmailCommand>
{
    public async Task<Result> Handle(SyncEmailCommand command, CancellationToken cancellationToken)
    {
        var email = Email.Create(command.Email);
        if (email.IsFailed)
        {
            return email.ToResult();
        }

        return await users.ApplyAsync(command.UserId, u => u.SyncEmail(email.Value, time), cancellationToken);
    }
}

/// <summary>A-23: <c>IdentityRoleChanged</c>; the role belongs to Auth, Accounts keeps a copy.</summary>
public sealed record SyncRoleCommand(Guid UserId, Contracts.ServerRole Role) : ICommand;

internal sealed class SyncRoleHandler(IUserRepository users, TimeProvider time) : ICommandHandler<SyncRoleCommand>
{
    public Task<Result> Handle(SyncRoleCommand command, CancellationToken cancellationToken) =>
        users.ApplyAsync(command.UserId, u => u.SyncRole(command.Role.ToDomain(), time), cancellationToken);
}

/// <summary>A-35: <c>IdentitySignedIn</c> moves <c>LastSeenAt</c> forward.</summary>
public sealed record RecordSignInCommand(Guid UserId, DateTimeOffset At) : ICommand;

internal sealed class RecordSignInHandler(IUserRepository users) : ICommandHandler<RecordSignInCommand>
{
    public Task<Result> Handle(RecordSignInCommand command, CancellationToken cancellationToken) =>
        users.ApplyAsync(command.UserId, u => u.RecordSeen(command.At), cancellationToken);
}

internal static class SyncExtensions
{
    public static async Task<Result> ApplyAsync(
        this IUserRepository users, Guid userId, Action<User> apply, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken);
        if (user is not null)
        {
            apply(user);
            return Result.Ok();
        }

        return await users.IsErasedAsync(userId, cancellationToken)
            ? Result.Ok()
            : Result.Fail(ApplicationErrors.UserPending);
    }
}
