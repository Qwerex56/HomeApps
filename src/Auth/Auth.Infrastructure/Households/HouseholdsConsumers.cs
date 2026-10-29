using Auth.Application.HouseholdsSync;
using HomeApps.Contracts.Households;
using MassTransit;
using MediatR;

namespace Auth.Infrastructure.Households;

/// <summary>Households D5a: only sends the command; the mail goes through Auth's outbox like every other email.</summary>
public sealed class OwnershipTransferredConsumer(ISender sender) : IConsumer<OwnershipTransferred>
{
    public Task Consume(ConsumeContext<OwnershipTransferred> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var m = context.Message;
        return sender.Send(new NotifyOwnershipReceivedCommand(m.HouseholdId, m.HouseholdName, m.ToUserId, m.Reason), context.CancellationToken);
    }
}
