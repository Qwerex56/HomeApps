using Households.Application.AccountsSync;
using Households.Application.Common;
using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;
using HomeApps.Contracts.Accounts;
using MassTransit;
using MediatR;

namespace Households.Infrastructure.Accounts;

/// <summary>
/// Households follows Accounts (S1–S4): each consumer only sends the command. The kit's inbox drops duplicates; the
/// commands are idempotent anyway. A failed result throws, so the message is retried (Q5) and, if it keeps failing,
/// ends in the <c>_error</c> queue instead of being lost.
/// </summary>
public sealed class UserCreatedConsumer(ISender sender) : IConsumer<UserCreated>
{
    public async Task Consume(ConsumeContext<UserCreated> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var m = context.Message;
        (await sender.Send(new CreateUserCopyCommand(m.UserId, m.DisplayName, m.AvatarUrl), context.CancellationToken)).ThrowIfFailed(m.UserId);
    }
}

public sealed class UserProfileUpdatedConsumer(ISender sender) : IConsumer<UserProfileUpdated>
{
    public async Task Consume(ConsumeContext<UserProfileUpdated> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var m = context.Message;
        (await sender.Send(new UpdateUserCopyCommand(m.UserId, m.DisplayName, m.AvatarUrl), context.CancellationToken)).ThrowIfFailed(m.UserId);
    }
}

public sealed class UserDisabledConsumer(ISender sender) : IConsumer<UserDisabled>
{
    public async Task Consume(ConsumeContext<UserDisabled> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var m = context.Message;
        (await sender.Send(new DisableUserCopyCommand(m.UserId, m.BySelf), context.CancellationToken)).ThrowIfFailed(m.UserId);
    }
}

public sealed class UserEnabledConsumer(ISender sender) : IConsumer<UserEnabled>
{
    public async Task Consume(ConsumeContext<UserEnabled> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var m = context.Message;
        (await sender.Send(new EnableUserCopyCommand(m.UserId), context.CancellationToken)).ThrowIfFailed(m.UserId);
    }
}

public sealed class UserErasedConsumer(ISender sender) : IConsumer<UserErased>
{
    public async Task Consume(ConsumeContext<UserErased> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var m = context.Message;
        (await sender.Send(new ForgetUserCommand(m.UserId), context.CancellationToken)).ThrowIfFailed(m.UserId);
    }
}

/// <summary>The event came before <c>UserCreated</c> (Q5): retried until the copy exists.</summary>
public sealed class UserNotYetKnownException(Guid userId)
    : Exception($"User {userId} is not known in Households yet; the message will be retried.")
{
    public Guid UserId { get; } = userId;
}

/// <summary>A command from an Accounts event failed for a reason other than ordering; the message must not be dropped.</summary>
public sealed class AccountsEventRejectedException(Guid userId, string errors)
    : Exception($"Households rejected an Accounts event for user {userId}: {errors}")
{
    public Guid UserId { get; } = userId;
}

internal static class ResultExtensions
{
    public static void ThrowIfFailed(this Result result, Guid userId)
    {
        if (result.IsSuccess)
        {
            return;
        }

        if (result.Errors.Any(e => e is AppError { Code: ApplicationErrors.UserPendingCode }))
        {
            throw new UserNotYetKnownException(userId);
        }

        throw new AccountsEventRejectedException(userId, string.Join("; ", result.Errors.Select(e => e.Message)));
    }
}
