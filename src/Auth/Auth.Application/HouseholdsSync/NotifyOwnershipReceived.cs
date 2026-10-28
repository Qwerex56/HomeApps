using Auth.Application.Abstractions;
using Auth.Application.Events;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeApps.Contracts.Households;

namespace Auth.Application.HouseholdsSync;

/// <summary>
/// Households D5a: <c>OwnershipTransferred</c>. Auth holds the email address, so it writes to the new Owner when the
/// household changed hands without them choosing it. A handover between members needs no mail; an unknown user
/// (erased in the meantime) is skipped.
/// </summary>
public sealed record NotifyOwnershipReceivedCommand(
    Guid HouseholdId, string HouseholdName, Guid ToUserId, OwnershipTransferReason Reason) : ICommand;

internal sealed class NotifyOwnershipReceivedHandler(IIdentityRepository identities, IEventBus bus)
    : ICommandHandler<NotifyOwnershipReceivedCommand>
{
    public async Task<Result> Handle(NotifyOwnershipReceivedCommand command, CancellationToken cancellationToken)
    {
        if (command.Reason == OwnershipTransferReason.HandedOver)
        {
            return Result.Ok();
        }

        var identity = await identities.GetByIdAsync(command.ToUserId, cancellationToken);
        if (identity is null)
        {
            return Result.Ok();
        }

        await bus.PublishAsync(
            new AuthEmailRequested(
                AuthEmailTemplate.HouseholdOwnershipReceived, identity.Email.Value, null,
                HouseholdId: command.HouseholdId,
                HouseholdName: command.HouseholdName,
                PreviousOwnerErased: command.Reason == OwnershipTransferReason.OwnerErased),
            cancellationToken);
        return Result.Ok();
    }
}
