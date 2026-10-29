using Accounts.Application.AuthSync;
using Accounts.Application.Common;
using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;
using HomeApps.Contracts.Auth;
using MassTransit;
using MediatR;

namespace Accounts.Infrastructure.Auth;

/// <summary>
/// Accounts follows Auth (accounts.md 4.1): each consumer only sends the command. The kit's inbox drops duplicates;
/// the commands are idempotent anyway. A failed result throws, so the message is retried (Q7) and, if it keeps
/// failing, ends in the <c>_error</c> queue instead of being lost.
/// </summary>
public sealed class IdentityRegisteredConsumer(ISender sender) : IConsumer<IdentityRegistered>
{
    public async Task Consume(ConsumeContext<IdentityRegistered> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        (await sender.Send(new RegisterUserCommand(context.Message), context.CancellationToken)).ThrowIfFailed(context.Message.UserId);
    }
}

public sealed class IdentityEmailChangedConsumer(ISender sender) : IConsumer<IdentityEmailChanged>
{
    public async Task Consume(ConsumeContext<IdentityEmailChanged> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var message = context.Message;
        (await sender.Send(new SyncEmailCommand(message.UserId, message.Email), context.CancellationToken)).ThrowIfFailed(message.UserId);
    }
}

public sealed class IdentityRoleChangedConsumer(ISender sender) : IConsumer<IdentityRoleChanged>
{
    public async Task Consume(ConsumeContext<IdentityRoleChanged> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var message = context.Message;
        (await sender.Send(new SyncRoleCommand(message.UserId, message.Role), context.CancellationToken)).ThrowIfFailed(message.UserId);
    }
}

public sealed class IdentitySignedInConsumer(ISender sender) : IConsumer<IdentitySignedIn>
{
    public async Task Consume(ConsumeContext<IdentitySignedIn> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var message = context.Message;
        (await sender.Send(new RecordSignInCommand(message.UserId, message.At), context.CancellationToken)).ThrowIfFailed(message.UserId);
    }
}

/// <summary>The event came before <c>IdentityRegistered</c> (Q7): retried until the account exists.</summary>
public sealed class UserNotYetRegisteredException(Guid userId)
    : Exception($"User {userId} is not registered in Accounts yet; the message will be retried.")
{
    public Guid UserId { get; } = userId;
}

/// <summary>A command from an Auth event failed for a reason other than ordering; the message must not be dropped.</summary>
public sealed class AuthEventRejectedException(Guid userId, string errors)
    : Exception($"Accounts rejected an Auth event for user {userId}: {errors}")
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
            throw new UserNotYetRegisteredException(userId);
        }

        throw new AuthEventRejectedException(userId, string.Join("; ", result.Errors.Select(e => e.Message)));
    }
}
